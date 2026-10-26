# Member Claims Demo
# Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-badr-ibrahim)
# Licensed under the MIT License. See LICENSE in the repository root.

"""
Load a CSV file of claims into SQL Server.

Flow (the same staging -> validate -> load pattern used in SSIS / ADF pipelines):
  1. Read the CSV with pandas.
  2. Clean and validate each row (required fields, dates, amounts, duplicates).
  3. Look up members by MemberNumber in SQL Server.
  4. Insert valid rows in one transaction; write rejected rows to a CSV with the reason.

Usage:
  python load_claims.py claims_sample.csv
  python load_claims.py claims_sample.csv --dry-run     # validate only, no database writes

Connection string comes from the MEMBER_CLAIMS_DB environment variable, or the default below
(matches docker-compose.yml).
"""

from __future__ import annotations

import argparse
import logging
import os
import sys
from datetime import date, datetime, timezone
from decimal import Decimal, InvalidOperation
from pathlib import Path

import pandas as pd

DEFAULT_CONNECTION = (
    "DRIVER={ODBC Driver 18 for SQL Server};"
    "SERVER=localhost,1433;DATABASE=MemberClaimsDemo;"
    "UID=sa;PWD=Demo_Passw0rd!;TrustServerCertificate=yes;Encrypt=yes"
)

REQUIRED_COLUMNS = ["member_number", "service_date", "amount", "description"]
MAX_AMOUNT = Decimal("100000")

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
log = logging.getLogger("load_claims")


def parse_amount(value: object) -> Decimal | None:
    """Money as Decimal (never float). Accepts '1,250.50' and '$85.00'."""
    try:
        text = str(value).replace("$", "").replace(",", "").strip()
        amount = Decimal(text).quantize(Decimal("0.01"))
    except (InvalidOperation, ValueError):
        return None
    return amount if Decimal("0.01") <= amount <= MAX_AMOUNT else None


def clean_and_validate(df: pd.DataFrame, today: date) -> tuple[pd.DataFrame, pd.DataFrame]:
    """Return (valid_rows, rejected_rows). Rejected rows get a 'reason' column."""
    missing = [c for c in REQUIRED_COLUMNS if c not in df.columns]
    if missing:
        raise ValueError(f"CSV is missing required columns: {', '.join(missing)}")

    df = df.copy()
    df["source_row"] = df.index + 2  # +2: header row and 1-based line numbers, for readable error reports
    df["member_number"] = df["member_number"].astype("string").str.strip().str.upper()
    df["description"] = df["description"].astype("string").str.strip()
    df["service_date"] = pd.to_datetime(df["service_date"], errors="coerce").dt.date
    df["amount"] = df["amount"].map(parse_amount)

    reasons = pd.Series("", index=df.index, dtype="string")
    reasons = reasons.mask(df["member_number"].isna() | (df["member_number"] == ""), "missing member_number")
    reasons = reasons.mask((reasons == "") & df["service_date"].isna(), "invalid service_date")
    reasons = reasons.mask(
        (reasons == "") & df["service_date"].map(lambda d: d is not None and not pd.isna(d) and d > today),
        "service_date is in the future",
    )
    reasons = reasons.mask((reasons == "") & df["amount"].isna(), "amount must be between 0.01 and 100000")
    reasons = reasons.mask(
        (reasons == "") & (df["description"].isna() | (df["description"].str.len() < 3)),
        "description must be at least 3 characters",
    )

    # Duplicate rows inside the same file (same member, date, amount, description): keep the first.
    key = ["member_number", "service_date", "amount", "description"]
    duplicates = df.duplicated(subset=key, keep="first")
    reasons = reasons.mask((reasons == "") & duplicates, "duplicate row in file")

    df["reason"] = reasons
    valid = df[df["reason"] == ""].drop(columns=["reason"])
    rejected = df[df["reason"] != ""]
    return valid, rejected


def load_to_sql(valid: pd.DataFrame, connection_string: str) -> tuple[int, pd.DataFrame]:
    """Insert valid claims in one transaction. Returns (inserted_count, rows_with_unknown_members)."""
    import pyodbc  # imported here so --dry-run works without the ODBC driver installed

    conn = pyodbc.connect(connection_string, autocommit=False)
    try:
        cursor = conn.cursor()

        # One query to look up all members (no N+1 lookups per row).
        numbers = sorted(set(valid["member_number"]))
        placeholders = ",".join("?" for _ in numbers)
        cursor.execute(
            f"SELECT MemberNumber, Id FROM Members WHERE Status = 'Active' AND MemberNumber IN ({placeholders})",
            numbers,
        )
        member_ids = {row.MemberNumber: row.Id for row in cursor.fetchall()}

        known = valid[valid["member_number"].isin(member_ids)]
        unknown = valid[~valid["member_number"].isin(member_ids)].assign(reason="unknown or inactive member")

        now = datetime.now(timezone.utc).replace(tzinfo=None)
        batch_id = now.strftime("%Y%m%d%H%M%S")
        rows = [
            (
                member_ids[r.member_number],
                f"CLM-IMP-{batch_id}-{i:05d}",
                r.service_date,
                r.amount,
                r.description,
                "Submitted",
                now,
            )
            for i, r in enumerate(known.itertuples(index=False), start=1)
        ]

        cursor.fast_executemany = True  # sends rows in bulk instead of one round trip each
        cursor.executemany(
            "INSERT INTO Claims (MemberId, ClaimNumber, ServiceDate, Amount, Description, Status, CreatedAtUtc) "
            "VALUES (?, ?, ?, ?, ?, ?, ?)",
            rows,
        )
        conn.commit()
        return len(rows), unknown
    except Exception:
        conn.rollback()  # all-or-nothing: no half-loaded files
        raise
    finally:
        conn.close()


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate and load a claims CSV into SQL Server.")
    parser.add_argument("csv_path", type=Path)
    parser.add_argument("--dry-run", action="store_true", help="validate only, don't write to the database")
    args = parser.parse_args(argv)

    df = pd.read_csv(args.csv_path, dtype=str)
    log.info("Read %d rows from %s", len(df), args.csv_path)

    valid, rejected = clean_and_validate(df, today=date.today())
    log.info("%d valid, %d rejected by validation", len(valid), len(rejected))

    if not args.dry_run and not valid.empty:
        connection_string = os.environ.get("MEMBER_CLAIMS_DB", DEFAULT_CONNECTION)
        inserted, unknown = load_to_sql(valid, connection_string)
        log.info("Inserted %d claims; %d rows had unknown or inactive members", inserted, len(unknown))
        rejected = pd.concat([rejected, unknown])

    if not rejected.empty:
        out = args.csv_path.with_name(args.csv_path.stem + "_rejected.csv")
        rejected.to_csv(out, index=False)
        log.warning("Wrote %d rejected rows with reasons to %s", len(rejected), out)

    return 0


if __name__ == "__main__":
    sys.exit(main())
