// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-badr-ibrahim)
// Licensed under the MIT License. See LICENSE in the repository root.

import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  Observable,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  startWith,
  switchMap,
} from 'rxjs';
import { MemberService, describeApiError } from '../member.service';
import { MemberSummary, PagedResult } from '../models';

interface SearchState {
  loading: boolean;
  result: PagedResult<MemberSummary> | null;
  error: string | null;
}

@Component({
  selector: 'app-member-search',
  imports: [ReactiveFormsModule, RouterLink, AsyncPipe],
  templateUrl: './member-search.component.html',
  styleUrl: './member-search.component.css',
})
export class MemberSearchComponent {
  private readonly members = inject(MemberService);

  readonly searchControl = new FormControl('', { nonNullable: true });

  /**
   * Search-as-you-type without flooding the API:
   *  - debounceTime(300): wait until the user pauses typing for 300 ms
   *  - distinctUntilChanged(): skip if the text didn't actually change
   *  - switchMap(): cancel the previous HTTP request when a new term arrives,
   *    so an older, slower response can never overwrite the latest results
   *  - catchError inside switchMap: one failed call doesn't kill the whole stream
   */
  readonly state$: Observable<SearchState> = this.searchControl.valueChanges.pipe(
    startWith(this.searchControl.value),
    debounceTime(300),
    map((value) => value.trim()),
    distinctUntilChanged(),
    switchMap((term) =>
      this.members.search(term).pipe(
        map((result): SearchState => ({ loading: false, result, error: null })),
        startWith<SearchState>({ loading: true, result: null, error: null }),
        catchError((error) => of<SearchState>({ loading: false, result: null, error: describeApiError(error) })),
      ),
    ),
  );
}
