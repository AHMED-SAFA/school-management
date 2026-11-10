export interface ClassInfo {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface CreateClassRequest {
  name: string;
  totalSeats: number;
}

export interface CreateClassResponse {
  id: string;
  name: string;
  totalSeats: number;
}

export interface HoldResponse {
  holdId: string;
  classId: string;
  studentId?: string;
  status: string;
  expiresAt: string;
}

export interface ConfirmResponse {
  holdId: string;
  status: string;
}

export interface MyHoldResponse {
  holdId: string;
  classId: string;
  className: string;
  status: string;
  expiresAt: string;
}

export interface ErrorResponse {
  error: string;
}
