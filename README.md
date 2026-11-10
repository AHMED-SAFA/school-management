# School Admission System (বিদ্যালয় ভর্তি ব্যবস্থা)

একটি নির্ভরযোগ্য, সমকালীন (concurrent) রিকোয়েস্ট-সহনশীল অনলাইন স্কুল ভর্তি সিস্টেম। এখানে "আগে আসলে আগে পাবেন" ভিত্তিতে সীমিত আসনের ক্লাসে শিক্ষার্থীরা আসন হোল্ড ও ভর্তি ফি জমা দিয়ে নিশ্চিত করতে পারে।

---

## ১. প্রযুক্তি স্ট্যাক (Tech Stack)

- **Backend**: C# (.NET 8 Web API), Entity Framework Core (PostgreSQL ORM)
- **Database**: PostgreSQL 16
- **Frontend**: React 18+, TypeScript, Vite, Material UI (MUI v6), Axios
- **Containerization**: Docker, Docker Compose

---

## ২. পরিবেশ কনফিগারেশন (.env Setup)

প্রজেক্টের সকল সংবেদনশীল তথ্য ও পাসওয়ার্ড কনফিগারেশনের জন্য `.env` ফাইল ব্যবহৃত হয়। 
নমুনা কনফিগারেশন দেখতে [.example.env](file:///e:/onnorokom/school-admission-system/.example.env) দেখুন।

`.env` ফাইলের বিষয়বস্তু:
```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=safa
POSTGRES_DB=school_admission
POSTGRES_PORT=5432

DATABASE_URL=Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=safa

HOLD_DURATION_SECONDS=600
BACKEND_PORT=8080

VITE_API_URL=http://localhost:8080
FRONTEND_PORT=3000
```

---

## ৩. Docker দিয়ে এক কমান্ডে চালানো (Quick Start with Docker)

পুরো অ্যাপ্লিকেশন (PostgreSQL ডাটাবেস, ব্যাকএন্ড ও ফ্রন্টএন্ড) একসাথে চালু করার জন্য কোনো কিছু আলাদা সেটআপ করার প্রয়োজন নেই। রিপোজিটরির রুটে নিচের কমান্ডটি চালান:

```bash
docker compose up --build
```

অথবা উইন্ডোজে:
```cmd
run-docker.bat
```
অথবা লিনাক্স/ম্যাক-এ:
```bash
chmod +x run-docker.sh && ./run-docker.sh
```

### অ্যাক্সেস লিঙ্কসমূহ (URLs):
- **Frontend UI**: [http://localhost:3000](http://localhost:3000)
- **Backend API**: [http://localhost:8080](http://localhost:8080)
- **Swagger API Docs**: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **PostgreSQL Database**: `localhost:5432`
  - **Database Name**: `school_admission`
  - **Username**: `postgres`
  - **Password**: `safa`
  *(স্থানীয় pgAdmin4 দিয়ে সরাসরি কানেক্ট করা যাবে)*

---

## ৪. স্থানীয় মেশিনে চালানো (Running Locally without Docker)

আপনার মেশিনে যদি .NET 8, Node.js এবং PostgreSQL থাকে:

### ক. ডাটাবেস সেটআপ
pgAdmin4 বা psql-এ `school_admission` নামে একটি ডাটাবেস তৈরি করুন।

### খ. ব্যাকএন্ড চালু করা
```bash
cd backend
dotnet run
```
ব্যাকএন্ড স্বয়ংক্রিয়ভাবে `.env` ফাইল থেকে পাসওয়ার্ড (`safa`) লোড করে ডাটাবেসে টেবিল মাইগ্রেশন সম্পন্ন করবে এবং `http://localhost:8080` এ চালু হবে।

### গ. ফ্রন্টএন্ড চালু করা
```bash
cd frontend
npm install
npm run dev
```
ফ্রন্টএন্ড `http://localhost:3000` এ চালু হবে।

---

## ৫. অটোমেটেড টেস্ট চালানো (Running Automated Tests)

### ক. ব্যাকএন্ড টেস্ট (কনকারেন্সি টেস্ট সহ)
ব্যাকএন্ডে ৬টি স্বয়ংসম্পূর্ণ টেস্ট রয়েছে, যার মধ্যে উচ্চ-চাপযুক্ত কনকারেন্সি টেস্ট অন্তর্ভুক্ত:
```bash
cd backend.Tests
dotnet test
```

#### টেস্টগুলোর বিবরণ:
1. `CreateClass_ReturnsCreatedClass`: নতুন শ্রেণি তৈরির এপিআই টেস্ট।
2. `HoldAndConfirmFlow_WorksCorrectly`: সিট হোল্ড করা এবং ফি দিয়ে নিশ্চিত করার পূর্ণাঙ্গ প্রবাহ।
3. `DuplicateHold_SameStudentSameClass_Returns409`: একই শ্রেণিতে একজন শিক্ষার্থীর দ্বিতীয়বার সক্রিয় আবেদন রোধ (409 Conflict)।
4. `CancelHold_ReturnsNoContent`: হোল্ড বাতিলের এপিআই টেস্ট (204 No Content)।
5. `GetMyHolds_ReturnsStudentHolds`: শিক্ষার্থীর সকল আবেদনের তালিকা দেখার টেস্ট।
6. `ConcurrencyTest_HoldsDoNotExceedTotalSeats`: একই সাথে বহু রিকোয়েস্ট পাঠিয়ে যাচাই করা যে মোট সিটের চেয়ে বেশি সিট কোনো অবস্থাতেই বরাদ্দ হয় না।

### খ. ফ্রন্টএন্ড টেস্ট
```bash
cd frontend
npm run test
```
১০টি ইউনিট টেস্ট ফ্রন্টএন্ডের ১৭-সংখ্যার জন্ম নিবন্ধন ভ্যালিডেশন, রিয়েল-টাইম কাউন্টডাউন, মেয়াদোত্তীর্ণের পর বাটন নিষ্ক্রিয়করণ এবং সিট হিসাব যাচাই করে।

---

## ৬. মূল বৈশিষ্ট্য ও নিয়মাবলী (Key Features & Rules)

1. **কঠোর কনকারেন্সি নিরাপত্তা**: PostgreSQL `Serializable` ট্রানজেকশন এবং রো-লেভেল পেসিমিস্টিক লকিং (`SELECT ... FOR UPDATE`) ব্যবহারের মাধ্যমে নিশ্চিত করা হয়েছে যে ব্যাকএন্ডের একাধিক প্রসেস সমান্তরালে চললেও `heldSeats + confirmedSeats` কখনোই `totalSeats` অতিক্রম করবে না।
2. **লগইনহীন কিন্তু সংরক্ষিত পরিচয়**: শিক্ষার্থীর ১৭-সংখ্যার জন্ম নিবন্ধন নম্বর পরিচয় হিসেবে ব্যবহৃত হয় এবং ব্রাউজারের `localStorage`-এ সংরক্ষিত থাকে। পেজ রিফ্রেশ বা ব্রাউজার পুনরায় খুললেও আবেদন অক্ষুণ্ণ থাকে।
3. **স্বয়ংক্রিয় আসন মুক্তি (Auto Expiry)**: `HOLD_DURATION_SECONDS` (ডিফল্ট: ৬০০ সেকেন্ড) পার হলেই সিট অন্য শিক্ষার্থীর জন্য উন্মুক্ত হয়। ফ্রন্টএন্ডের টাইমার শেষ হওয়া মাত্র পেজ রিলোড ছাড়াই `EXPIRED` প্রদর্শিত হয় এবং ফি জমা বাটন বন্ধ হয়ে যায়।
4. **সিট সংখ্যার রিয়েলটাইম আপডেট**: পেজ রিলোড ছাড়াই প্রতি কয়েক সেকেন্ড পর পর শ্রেণি তালিকার সিট সংখ্যা হালনাগাদ হয়।
5. **সহজবোধ্য ত্রুটি বার্তা**: সিট শেষ, ভুল ইনপুট বা মেয়াদ শেষের মতো যেকোনো পরিস্থিতিতে ব্যবহারকারীর বোধগম্য বার্তা প্রদর্শিত হয়।

---

## ৭. ফাইল ডিরেক্টরি পরিচিতি (Directory Structure)

```
school-admission-system/
├── backend/                  # .NET 8 Web API
│   ├── Controllers/          # REST API Controllers (Classes, Holds)
│   ├── Data/                 # EF Core DbContext & Migrations
│   ├── Models/               # Entities & DTOs
│   ├── Services/             # Business Logic & Concurrency Control
│   ├── Dockerfile            # Multi-stage Docker build for backend
│   ├── .env                  # Backend environment config
│   ├── .example.env          # Backend template
│   └── Program.cs
├── backend.Tests/            # xUnit automated tests & Concurrency test
├── frontend/                 # React 18 + TS + Vite + MUI
│   ├── src/
│   │   ├── api/              # Axios API client with X-Student-Id
│   │   ├── components/       # ClassList, MyHolds, CreateClassDialog
│   │   └── tests/            # Vitest frontend tests
│   ├── Dockerfile            # Frontend container setup
│   ├── .env                  # Frontend environment config
│   └── .example.env          # Frontend template
├── .env                      # Root environment configuration
├── .example.env              # Root environment template
├── docker-compose.yml        # Multi-container orchestration (DB, API, UI)
├── run-docker.bat            # Windows quick-run script
├── run-docker.sh             # Linux/macOS quick-run script
├── ASSUMPTIONS.md            # Design decisions & multi-process concurrency
├── AI_USAGE.md               # AI assistance & verification disclosure
└── README.md                 # Project documentation & run guide
```
