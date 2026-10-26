// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
// Licensed under the MIT License. See LICENSE in the repository root.

import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Claim, CreateClaimRequest, MemberDetail, MemberSummary, PagedResult } from './models';

/**
 * All calls to the ASP.NET Core API live here, so components stay focused on the UI.
 * Angular's dependency injection provides one instance app-wide (providedIn: 'root'),
 * the same idea as a singleton service in ASP.NET Core.
 */
@Injectable({ providedIn: 'root' })
export class MemberService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/members'; // proxied to http://localhost:5080 (proxy.conf.json)

  search(term: string, page = 1, pageSize = 20): Observable<PagedResult<MemberSummary>> {
    const params = new HttpParams()
      .set('search', term)
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http.get<PagedResult<MemberSummary>>(this.baseUrl, { params });
  }

  getMember(id: number): Observable<MemberDetail> {
    return this.http.get<MemberDetail>(`${this.baseUrl}/${id}`);
  }

  addClaim(memberId: number, request: CreateClaimRequest): Observable<Claim> {
    return this.http.post<Claim>(`${this.baseUrl}/${memberId}/claims`, request);
  }
}

/** Turns an API error (ProblemDetails or validation errors) into one readable message. */
export function describeApiError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error;
    if (body?.errors) {
      // ASP.NET Core validation response: { errors: { Field: ["message"] } }
      return Object.values(body.errors as Record<string, string[]>).flat().join(' ');
    }
    if (body?.detail) {
      return body.detail;
    }
    if (error.status === 0) {
      return 'The API is not reachable. Is it running on http://localhost:5080?';
    }
    return body?.title ?? `Request failed (${error.status}).`;
  }
  return 'Something went wrong.';
}
