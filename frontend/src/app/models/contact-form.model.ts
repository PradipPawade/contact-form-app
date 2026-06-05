export interface ContactFormModel {
  firstName: string;
  lastName:  string;
  email:     string;
  phone:     string;
  subject:   string;
  message:   string;
}

export interface ContactFormResponse {
  success:     boolean;
  message:     string;
  referenceId: string;
  submittedAt: string;
}
