import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ContactFormModel, ContactFormResponse, ContactSubmission } from '../models/contact-form.model';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly base = `${environment.apiBaseUrl}/contact`;

  constructor(private http: HttpClient) {}

  submit(form: ContactFormModel): Observable<ContactFormResponse> {
    return this.http.post<ContactFormResponse>(`${this.base}/submit`, form);
  }

  getAll(): Observable<ContactSubmission[]> {
    return this.http.get<ContactSubmission[]>(`${this.base}/list`);
  }
}
