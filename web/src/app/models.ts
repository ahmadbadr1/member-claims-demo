// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-badr-ibrahim)
// Licensed under the MIT License. See LICENSE in the repository root.

// Typed models that mirror the API's DTOs (MemberClaims.Api/Dtos).

export type MemberStatus = 'Active' | 'Inactive';
export type ClaimStatus = 'Submitted' | 'Approved' | 'Denied';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface MemberSummary {
  id: number;
  memberNumber: string;
  fullName: string;
  employerName: string;
  status: MemberStatus;
  claimCount: number;
}

export interface Claim {
  id: number;
  claimNumber: string;
  serviceDate: string; // yyyy-MM-dd
  amount: number;
  description: string;
  status: ClaimStatus;
  createdAtUtc: string;
}

export interface MemberDetail {
  id: number;
  memberNumber: string;
  firstName: string;
  lastName: string;
  employerName: string;
  status: MemberStatus;
  enrolledOn: string;
  totalClaimed: number;
  claims: Claim[];
}

export interface CreateClaimRequest {
  serviceDate: string;
  amount: number;
  description: string;
}
