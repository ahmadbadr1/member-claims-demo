// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
// Licensed under the MIT License. See LICENSE in the repository root.

import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MemberService, describeApiError } from '../member.service';
import { MemberDetail } from '../models';

@Component({
  selector: 'app-member-detail',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './member-detail.component.html',
})
export class MemberDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly members = inject(MemberService);
  private readonly fb = inject(FormBuilder);

  private readonly memberId = Number(this.route.snapshot.paramMap.get('id'));

  // Signals hold the component state; the template re-renders when they change.
  readonly member = signal<MemberDetail | null>(null);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly saving = signal(false);
  readonly savedMessage = signal<string | null>(null);

  readonly today = new Date().toISOString().slice(0, 10);

  // Reactive form with client-side validation (the API validates again - never trust the client).
  readonly claimForm = this.fb.group({
    serviceDate: ['', Validators.required],
    amount: [null as number | null, [Validators.required, Validators.min(0.01), Validators.max(100000)]],
    description: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loadError.set(null);
    this.members.getMember(this.memberId).subscribe({
      next: (member) => this.member.set(member),
      error: (error) => this.loadError.set(describeApiError(error)),
    });
  }

  submitClaim(): void {
    if (this.claimForm.invalid) {
      this.claimForm.markAllAsTouched();
      return;
    }

    const { serviceDate, amount, description } = this.claimForm.getRawValue();
    this.saving.set(true);
    this.saveError.set(null);
    this.savedMessage.set(null);

    this.members
      .addClaim(this.memberId, {
        serviceDate: serviceDate!,
        amount: amount!,
        description: description!,
      })
      .subscribe({
        next: (claim) => {
          this.saving.set(false);
          this.savedMessage.set(`Claim ${claim.claimNumber} submitted.`);
          this.claimForm.reset();
          this.load();
        },
        error: (error) => {
          this.saving.set(false);
          this.saveError.set(describeApiError(error));
        },
      });
  }

  showError(controlName: 'serviceDate' | 'amount' | 'description'): boolean {
    const control = this.claimForm.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }
}
