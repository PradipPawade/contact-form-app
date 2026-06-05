import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ContactFormModel, ContactFormResponse } from '../models/contact-form.model';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly endpoint = `${environment.apiBaseUrl}/contact/submit`;

  constructor(private http: HttpClient) {}

  submit(form: ContactFormModel): Observable<ContactFormResponse> {
    return this.http.post<ContactFormResponse>(this.endpoint, form);
  }
}
