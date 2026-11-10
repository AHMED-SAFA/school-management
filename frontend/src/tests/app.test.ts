import { describe, it, expect } from 'vitest';

describe('Frontend Business Logic Tests', () => {
  describe('Student ID Validation', () => {
    const validateStudentId = (id: string): { valid: boolean; error?: string } => {
      const trimmed = id.trim();
      if (!trimmed) {
        return { valid: false, error: 'Birth Registration Number is required.' };
      }
      if (!/^\d{17}$/.test(trimmed)) {
        return { valid: false, error: 'Birth Registration Number must be exactly 17 digits.' };
      }
      return { valid: true };
    };

    it('validates a correct 17-digit student birth registration number', () => {
      const result = validateStudentId('20152691234567890');
      expect(result.valid).toBe(true);
      expect(result.error).toBeUndefined();
    });

    it('rejects an empty student ID', () => {
      const result = validateStudentId('');
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Birth Registration Number is required.');
    });

    it('rejects a student ID with fewer than 17 digits', () => {
      const result = validateStudentId('1234567890123456');
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Birth Registration Number must be exactly 17 digits.');
    });

    it('rejects a student ID containing non-numeric characters', () => {
      const result = validateStudentId('2015269123456789A');
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Birth Registration Number must be exactly 17 digits.');
    });
  });

  describe('Hold Expiration & Countdown Logic', () => {
    const isHoldExpired = (expiresAt: string, currentTime: number): boolean => {
      return new Date(expiresAt).getTime() <= currentTime;
    };

    const formatCountdown = (expiresAt: string, currentTime: number): string => {
      const diff = new Date(expiresAt).getTime() - currentTime;
      if (diff <= 0) return 'Expired';
      const minutes = Math.floor(diff / 60000);
      const seconds = Math.floor((diff % 60000) / 1000);
      return `${minutes}m ${seconds}s`;
    };

    it('correctly detects when a hold has expired', () => {
      const past = new Date(Date.now() - 5000).toISOString();
      expect(isHoldExpired(past, Date.now())).toBe(true);
    });

    it('correctly detects when a hold is still active', () => {
      const future = new Date(Date.now() + 60000).toISOString();
      expect(isHoldExpired(future, Date.now())).toBe(false);
    });

    it('formats remaining countdown correctly', () => {
      const now = 1000000;
      const expiresAt = new Date(now + 125000).toISOString(); // 2m 5s
      expect(formatCountdown(expiresAt, now)).toBe('2m 5s');
    });

    it('returns "Expired" when countdown reaches 0 or negative', () => {
      const now = 1000000;
      const expiresAt = new Date(now - 1000).toISOString();
      expect(formatCountdown(expiresAt, now)).toBe('Expired');
    });
  });

  describe('Seat Calculation Logic', () => {
    const calculateSeats = (total: number, held: number, confirmed: number) => {
      const available = total - held - confirmed;
      return {
        total,
        held,
        confirmed,
        available: Math.max(0, available),
      };
    };

    it('calculates available seats properly', () => {
      const seats = calculateSeats(120, 15, 80);
      expect(seats.available).toBe(25);
    });

    it('does not allow available seats to be negative', () => {
      const seats = calculateSeats(10, 5, 5);
      expect(seats.available).toBe(0);
    });
  });
});
