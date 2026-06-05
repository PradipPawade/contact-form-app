import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ContactFormModel, ContactFormResponse, ContactSubmission } from '../models/contact-form.model';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly base = `${environment.apiBaseUrl}/contact`;

  constructor(private http: HttpClient) {}

  submit(form: ContactFormModel, attachment?: File | null): Observable<ContactFormResponse> {
    // Use FormData to support file upload
    const fd = new FormData();
    fd.append('firstName', form.firstName);
    fd.append('lastName',  form.lastName);
    fd.append('email',     form.email);
    fd.append('phone',     form.phone     ?? '');
    fd.append('subject',   form.subject);
    fd.append('message',   form.message);

    if (attachment) {
      fd.append('attachment', attachment, attachment.name);
    }

    return this.http.post<ContactFormResponse>(`${this.base}/submit`, fd);
  }

  getAll(): Observable<ContactSubmission[]> {
    return this.http.get<ContactSubmission[]>(`${this.base}/list`);
  }
}
