# ASSUMPTIONS & DESIGN DECISIONS

## 1. Identity & Authentication
- As explicitly instructed in the project brief, no password-based login system is used.
- Student identity is represented by their 17-digit Birth Registration Number, transmitted via the `X-Student-Id` request header across all applicable API endpoints.
- In the frontend UI, the entered birth registration number is stored in the browser's `localStorage`. As a result, page reloads or browser restarts preserve the student's active identity and ongoing applications without losing session context.

## 2. Class & Shift Model
- Each shift of a class is treated as an independent "class" instance with its own unique identifier and dedicated seat quota (e.g., "Class 6 - Morning Shift" and "Class 6 - Day Shift" are two separate class entities).
- Each class tracks its own `totalSeats`, `heldSeats`, `confirmedSeats`, and `availableSeats`.

## 3. Seat Holds & Automatic Expiration
- The seat hold expiration duration is governed by the `HOLD_DURATION_SECONDS` environment variable (default: 600 seconds / 10 minutes).
- Expired holds are released reactively at the database level using timestamp filtering (`ExpiresAt <= DateTime.UtcNow`). This eliminates the need for external background cron jobs or daemon processes to free up abandoned seats.
- The frontend maintains an active per-second interval timer. When the countdown reaches 0, the hold state transitions to `EXPIRED` immediately without requiring a page refresh, and the "Pay & Confirm" action button is disabled.

## 4. Multi-Process Concurrency Safety (Bonus §1.6)
- When multiple backend processes or containers operate simultaneously on distinct ports against the same database, seat overbooking is strictly prevented at the database engine level:
  1. **PostgreSQL Serializable Transactions**: Transactions run under the `Serializable` isolation level.
  2. **Pessimistic Row-Level Locking (`SELECT ... FOR UPDATE`)**: During seat hold creation, the target class row is locked using `FOR UPDATE`, ensuring no other process can concurrently inspect or mutate seat counts for that class.
  3. **Atomic Quota Validation**: Current `heldSeats` and `confirmedSeats` are evaluated only after acquiring the row lock. If `held + confirmed >= totalSeats`, a `409 Conflict` response is immediately returned.
  4. **Idempotent Confirmation**: Row-level locking on the hold record guarantees that duplicate or concurrent confirmation requests increment `confirmedSeats` exactly once.

## 5. Payments & Cancellations
- Clicking "Pay & Confirm" simulates a successful payment transaction without integrating an external payment gateway.
- Only unexpired `HELD` applications belonging to the requesting student can be confirmed. Attempting to confirm or cancel an application belonging to another student returns a `403 Forbidden` response.
- Once an admission is confirmed (`CONFIRMED`), it cannot be canceled.
