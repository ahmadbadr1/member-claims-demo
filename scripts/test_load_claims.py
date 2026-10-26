# Member Claims Demo
# Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
# Licensed under the MIT License. See LICENSE in the repository root.

from datetime import date
from decimal import Decimal

import pandas as pd

from load_claims import clean_and_validate, parse_amount

TODAY = date(2026, 10, 24)


def test_parse_amount_handles_currency_text():
    assert parse_amount("$1,250.50") == Decimal("1250.50")
    assert parse_amount("64.2") == Decimal("64.20")
    assert parse_amount("-15") is None
    assert parse_amount("abc") is None


def test_sample_file_splits_into_valid_and_rejected():
    df = pd.read_csv("claims_sample.csv", dtype=str)

    valid, rejected = clean_and_validate(df, TODAY)

    # 11 rows: 5 valid (M-999999 is rejected later by the database member lookup), 6 rejected here.
    assert len(valid) == 5
    assert set(rejected["reason"]) == {
        "duplicate row in file",
        "invalid service_date",
        "amount must be between 0.01 and 100000",
        "missing member_number",
        "description must be at least 3 characters",
        "service_date is in the future",
    }
    # Member numbers are trimmed and upper-cased.
    assert "M-100002" in set(valid["member_number"])
