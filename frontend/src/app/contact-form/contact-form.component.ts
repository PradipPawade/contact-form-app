import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ContactService } from '../services/contact.service';
import { ContactFormResponse } from '../models/contact-form.model';

type FormState = 'idle' | 'submitting' | 'success' | 'error';

@Component({
  selector: 'app-contact-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact-form.component.html',
  styleUrls: ['./contact-form.component.css']
})
export class ContactFormComponent {
  form: FormGroup;
  state = signal<FormState>('idle');
  serverMessage = signal<string>('');
  referenceId   = signal<string>('');
  serverErrors  = signal<Record<string, string[]>>({});

  constructor(private fb: FormBuilder, private contactService: ContactService) {
    this.form = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName:  ['', [Validators.required, Validators.maxLength(50)]],
      email:     ['', [Validators.required, Validators.email]],
      phone:     ['', [Validators.pattern(/^\+?[\d\s\-\(\)]{7,20}$/)]],
      subject:   ['', [Validators.required, Validators.maxLength(100)]],
      message:   ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]]
    });
  }

  get f() { return this.form.controls; }

  fieldError(name: string): string | null {
    const ctrl = this.form.get(name);
    if (!ctrl || (!ctrl.dirty && !ctrl.touched)) return null;
    if (ctrl.hasError('required'))    return 'This field is required.';
    if (ctrl.hasError('email'))       return 'Enter a valid email address.';
    if (ctrl.hasError('maxlength'))   return `Max ${ctrl.errors!['maxlength'].requiredLength} characters.`;
    if (ctrl.hasError('minlength'))   return `Min ${ctrl.errors!['minlength'].requiredLength} characters.`;
    if (ctrl.hasError('pattern'))     return 'Invalid format.';
    const srv = this.serverErrors()[name];
    return srv ? srv[0] : null;
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.state.set('submitting');
    this.serverErrors.set({});

    this.contactService.submit(this.form.value).subscribe({
      next: (res: ContactFormResponse) => {
        this.state.set('success');
        this.serverMessage.set(res.message);
        this.referenceId.set(res.referenceId);
        this.form.reset();
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 400 && err.error?.errors) {
          this.serverErrors.set(err.error.errors);
          this.state.set('idle');
        } else {
          this.state.set('error');
          this.serverMessage.set('An unexpected error occurred. Please try again later.');
        }
      }
    });
  }

  reset(): void {
    this.state.set('idle');
    this.serverMessage.set('');
    this.referenceId.set('');
    this.serverErrors.set({});
    this.form.reset();
  }
}
