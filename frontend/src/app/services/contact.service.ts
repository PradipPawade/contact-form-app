import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ContactFormModel, ContactFormResponse, ContactSubmission, UploadUrlResponse } from '../models/contact-form.model';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly base = `${environment.apiBaseUrl}/contact`;

  constructor(private http: HttpClient) {}

  /** Step 1: Get a short-lived SAS URL for direct browser → blob upload */
  getUploadUrl(filename: string): Observable<UploadUrlResponse> {
    return this.http.get<UploadUrlResponse>(
      `${this.base}/upload-url?filename=${encodeURIComponent(filename)}`
    );
  }

  /** Step 2: Upload file directly to Azure Blob Storage using the SAS URL */
  uploadToBlobStorage(sasUrl: string, file: File): Observable<void> {
    const headers = new HttpHeaders({ 'x-ms-blob-type': 'BlockBlob' });
    return this.http.put<void>(sasUrl, file, { headers });
  }

  /** Step 3: Submit form JSON (with blobUrl if file was uploaded) */
  submit(form: ContactFormModel): Observable<ContactFormResponse> {
    return this.http.post<ContactFormResponse>(`${this.base}/submit`, form);
  }

  getAll(): Observable<ContactSubmission[]> {
    return this.http.get<ContactSubmission[]>(`${this.base}/list`);
  }
}
