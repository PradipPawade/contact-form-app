import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ContactService } from '../services/contact.service';
import { ContactFormResponse, ContactSubmission } from '../models/contact-form.model';

type FormState = 'idle' | 'submitting' | 'success' | 'error';

@Component({
  selector: 'app-contact-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact-form.component.html',
  styleUrls: ['./contact-form.component.css']
})
export class ContactFormComponent implements OnInit {
  form: FormGroup;
  state          = signal<FormState>('idle');
  serverMessage  = signal<string>('');
  referenceId    = signal<string>('');
  serverErrors   = signal<Record<string, string[]>>({});
  submissions    = signal<ContactSubmission[]>([]);
  loadingList    = signal<boolean>(false);
  selectedFile   = signal<File | null>(null);
  fileError      = signal<string>('');

  readonly allowedTypes = '.pdf,.doc,.docx,.jpg,.jpeg,.png,.txt';
  readonly maxSizeMB    = 5;

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

  ngOnInit(): void { this.loadSubmissions(); }

  loadSubmissions(): void {
    this.loadingList.set(true);
    this.contactService.getAll().subscribe({
      next: (data) => { this.submissions.set(data); this.loadingList.set(false); },
      error: ()    => { this.loadingList.set(false); }
    });
  }

  get f() { return this.form.controls; }

  fieldError(name: string): string | null {
    const ctrl = this.form.get(name);
    if (!ctrl || (!ctrl.dirty && !ctrl.touched)) return null;
    if (ctrl.hasError('required'))  return 'This field is required.';
    if (ctrl.hasError('email'))     return 'Enter a valid email address.';
    if (ctrl.hasError('maxlength')) return `Max ${ctrl.errors!['maxlength'].requiredLength} characters.`;
    if (ctrl.hasError('minlength')) return `Min ${ctrl.errors!['minlength'].requiredLength} characters.`;
    if (ctrl.hasError('pattern'))   return 'Invalid format.';
    const srv = this.serverErrors()[name];
    return srv ? srv[0] : null;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0] ?? null;
    this.fileError.set('');

    if (!file) { this.selectedFile.set(null); return; }

    // Validate size (5 MB)
    if (file.size > this.maxSizeMB * 1024 * 1024) {
      this.fileError.set(`File must be under ${this.maxSizeMB} MB.`);
      this.selectedFile.set(null);
      input.value = '';
      return;
    }

    this.selectedFile.set(file);
  }

  removeFile(): void {
    this.selectedFile.set(null);
    this.fileError.set('');
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024)        return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.state.set('submitting');
    this.serverErrors.set({});

    const file = this.selectedFile();

    if (file) {
      // Step 1: Get SAS URL
      this.contactService.getUploadUrl(file.name).subscribe({
        next: ({ uploadUrl, blobUrl }) => {
          // Step 2: Upload directly to blob storage
          this.contactService.uploadToBlobStorage(uploadUrl, file).subscribe({
            next: () => {
              // Step 3: Submit form with blob URL
              this.submitForm(blobUrl, file.name);
            },
            error: () => {
              this.state.set('error');
              this.serverMessage.set('File upload failed. Please try again.');
            }
          });
        },
        error: (err: HttpErrorResponse) => {
          this.state.set('error');
          this.serverMessage.set(err.error?.error ?? 'Could not get upload URL. Please try again.');
        }
      });
    } else {
      // No file — submit directly
      this.submitForm(null, null);
    }
  }

  private submitForm(attachmentUrl: string | null, attachmentName: string | null): void {
    const payload = {
      ...this.form.value,
      attachmentUrl:  attachmentUrl  ?? null,
      attachmentName: attachmentName ?? null
    };

    this.contactService.submit(payload).subscribe({
      next: (res: ContactFormResponse) => {
        this.state.set('success');
        this.serverMessage.set(res.message);
        this.referenceId.set(res.referenceId);
        this.form.reset();
        this.selectedFile.set(null);
        this.loadSubmissions();
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 400 && err.error?.errors) {
          this.serverErrors.set(err.error.errors);
          this.state.set('idle');
        } else {
          this.state.set('error');
          this.serverMessage.set(err.error?.error ?? 'An unexpected error occurred. Please try again later.');
        }
      }
    });
  }

  reset(): void {
    this.state.set('idle');
    this.serverMessage.set('');
    this.referenceId.set('');
    this.serverErrors.set({});
    this.selectedFile.set(null);
    this.fileError.set('');
    this.form.reset();
  }
}
