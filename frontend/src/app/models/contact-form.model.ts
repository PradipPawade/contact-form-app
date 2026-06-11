export interface ContactFormModel {
  firstName:      string;
  lastName:       string;
  email:          string;
  phone:          string;
  subject:        string;
  message:        string;
  attachmentUrl?:  string | null;
  attachmentName?: string | null;
}

export interface UploadUrlResponse {
  uploadUrl: string;
  blobUrl:   string;
}

export interface ContactFormResponse {
  success:     boolean;
  message:     string;
  referenceId: string;
  submittedAt: string;
}

export interface ContactSubmission {
  id:             number;
  firstName:      string;
  lastName:       string;
  email:          string;
  phone:          string;
  subject:        string;
  message:        string;
  referenceId:    string;
  submittedAt:    string;
  attachmentUrl:  string | null;
  attachmentName: string | null;
}
