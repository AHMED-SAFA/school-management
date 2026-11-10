# School Admission System

A robust, highly concurrent online school admission system built with **.NET 8**, **PostgreSQL**, **React 18+ (TypeScript & Vite)**, and **Docker**. Admissions operate on a strict **first-come, first-served** basis for limited seats per class shift. Applicants can hold seats temporarily, pay admission fees to confirm their admission, or let seats automatically release upon expiration.

---

## 1. Tech Stack

- **Backend**: C# (.NET 8 Web API), Entity Framework Core (PostgreSQL ORM)
- **Database**: PostgreSQL 16
- **Frontend**: React 18+, TypeScript, Vite, Material UI (MUI v6), Axios
- **Containerization**: Docker, Docker Compose

---

## 2. Environment Configuration (.env Setup)

All confidential credentials and environment variables are strictly decoupled using `.env` files.
See [.example.env](.example.env) for the template.

Sample `.env` file contents:
```env
# ==========================================
# Database Configuration (PostgreSQL)
# ==========================================
POSTGRES_USER=postgres
POSTGRES_PASSWORD=
POSTGRES_DB=school_admission
POSTGRES_PORT=5432

# Backend Connection String (For local development or container)
DATABASE_URL=Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=safa

# ==========================================
# Backend Configuration
# ==========================================
HOLD_DURATION_SECONDS=600
BACKEND_PORT=8080

# ==========================================
# Frontend Configuration
# ==========================================
VITE_API_URL=http://localhost:8080
FRONTEND_PORT=3000
```

> **Security Note:** `.env` files are ignored by git (`.gitignore`). Use `.example.env` as a reference when configuring new environments.

---

## 3. Quick Start with Docker (Single Command)

To run the entire system (PostgreSQL database, Backend API, and Frontend UI) with a single command:

```bash
docker compose up --build
```

Or on Windows:
```cmd
run-docker.bat
```
Or on Linux/macOS:
```bash
chmod +x run-docker.sh && ./run-docker.sh
```

### Access URLs:
- **Frontend UI**: [http://localhost:3000](http://localhost:3000)
- **Backend API**: [http://localhost:8080](http://localhost:8080)
- **Swagger API Docs**: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **PostgreSQL Database**: `localhost:5432`
  - **Database Name**: `school_admission`
  - **Username**: `postgres`
  - **Password**: 

---

## 4. Running Locally without Docker

If you have .NET 8 SDK, Node.js (18+), and PostgreSQL installed on your machine:

### Step 1: Database Setup
Create a database named `school_admission` in PostgreSQL (using pgAdmin 4 or `psql`).

### Step 2: Run Backend
```bash
cd backend
dotnet run
```
The backend automatically reads credentials from `.env`, runs database migrations, and starts listening at `http://localhost:8080`.

### Step 3: Run Frontend
```bash
cd frontend
npm install
npm run dev
```
The frontend interface starts at `http://localhost:3000`.

---

## 5. Running Automated Tests

### A. Backend Tests (Includes High-Load Concurrency Test)
The backend test suite includes 6 automated tests verifying business logic, API contracts, race conditions, and concurrency safety:

```bash
cd backend.Tests
dotnet test
```

#### Test Suite Overview:
1. `CreateClass_ReturnsCreatedClass`: Verifies class creation endpoint.
2. `HoldAndConfirmFlow_WorksCorrectly`: Tests end-to-end seat hold, payment confirmation, and seat count deduction.
3. `DuplicateHold_SameStudentSameClass_Returns409`: Enforces single active application per student per class (`409 Conflict`).
4. `CancelHold_ReturnsNoContent`: Tests hold cancellation (`204 No Content`) and seat restoration.
5. `GetMyHolds_ReturnsStudentHolds`: Verifies retrieving all applications for a student ID.
6. `ConcurrencyTest_HoldsDoNotExceedTotalSeats`: Dispatches 20 simultaneous concurrent requests against a class with limited seats to prove that `held + confirmed` strictly never exceeds `totalSeats`.

### B. Frontend Tests
Unit tests for validation, live timer formatting, auto-expiration, and seat calculations:

```bash
cd frontend
npm run test
```
All 10 Vitest unit tests pass.

---

## 6. Key Features & Business Rules

1. **Strict Multi-Process Concurrency Safety (Bonus §1.6)**:
   - Uses PostgreSQL **`Serializable` transaction isolation** combined with pessimistic row-level locking (**`SELECT ... FOR UPDATE`**).
   - Even when multiple backend server processes run concurrently across different ports or containers against the same database, `heldSeats + confirmedSeats` will never exceed `totalSeats`.
2. **Loginless Persistent Student Identity**:
   - The student's 17-digit Birth Registration Number is used as the identity header (`X-Student-Id`).
   - Saved in browser `localStorage` so refreshing or reopening the browser preserves active applications.
3. **Automatic Seat Release (Auto-Expiry)**:
   - Hold durations default to 600 seconds (`HOLD_DURATION_SECONDS`).
   - Expired holds are released reactively on every status evaluation without requiring background daemons.
   - The UI runs a real-time countdown; upon reaching 0, status automatically changes to `EXPIRED` and the "Pay & Confirm" button disables without page reload.
4. **Live Seat Updates**:
   - Class seat counts update periodically in the UI without requiring page reload.
5. **Idempotent Confirmations**:
   - Multiple confirm calls for the same hold confirm once and do not inflate confirmed seat counts.
6. **Graceful User-Friendly Errors**:
   - Returns clear, actionable alert messages for duplicate applications, exhausted seats, or expired holds.

---

## 7. API Specification

Base URL: `http://localhost:8080`

| Method | Path | Required Headers | Description | Status Code |
| :--- | :--- | :--- | :--- | :---: |
| `POST` | `/classes` | - | Create new class | `201 Created` |
| `GET` | `/classes` | - | List all classes with seat counts | `200 OK` |
| `GET` | `/classes/{classId}` | - | Get seat status of a specific class | `200 OK` |
| `POST` | `/classes/{classId}/holds` | `X-Student-Id: <17_digits>` | Hold seat for student (409 if full/duplicate) | `201 Created` |
| `GET` | `/holds/{holdId}` | - | Check status of a hold | `200 OK` |
| `POST` | `/holds/{holdId}/confirm` | `X-Student-Id: <17_digits>` | Pay fee and confirm admission | `200 OK` |
| `DELETE`| `/holds/{holdId}` | `X-Student-Id: <17_digits>` | Cancel hold | `204 No Content` |
| `GET` | `/me/holds` | `X-Student-Id: <17_digits>` | List all applications for student | `200 OK` |

---

## 8. Directory Structure

```text
school-admission-system/
├── backend/                  # .NET 8 Web API
│   ├── Controllers/          # REST API Controllers (Classes, Holds)
│   ├── Data/                 # EF Core DbContext & Migrations
│   ├── Models/               # Entities & DTOs
│   ├── Services/             # Business Logic & Concurrency Control
│   ├── Dockerfile            # Multi-stage Docker build for backend
│   ├── .env                  # Backend environment variables
│   ├── .example.env          # Backend environment template
│   └── Program.cs            # App configuration & .env loader
├── backend.Tests/            # xUnit tests & Concurrency test suite
├── frontend/                 # React 18 + TS + Vite + MUI
│   ├── src/
│   │   ├── api/              # Axios API client with X-Student-Id
│   │   ├── components/       # ClassList, MyHolds, CreateClassDialog
│   │   ├── tests/            # Vitest unit tests
│   │   ├── types/            # TypeScript data interfaces
│   │   ├── App.tsx           # Main UI container & state
│   │   └── main.tsx
│   ├── Dockerfile            # Frontend container setup
│   ├── .env                  # Frontend environment variables
│   ├── .example.env          # Frontend environment template
│   ├── package.json
│   └── vite.config.ts
├── .env                      # Root environment configuration
├── .example.env              # Root environment template
├── .gitignore                # Git exclusions (credentials, build artifacts)
├── docker-compose.yml        # Multi-container orchestration (DB, API, UI)
├── run-docker.bat            # Windows one-click run script
├── run-docker.sh             # Linux/macOS one-click run script
├── ASSUMPTIONS.md            # Technical assumptions & design choices
├── AI_USAGE.md               # AI tooling transparency report
└── README.md                 # Complete project documentation
```
