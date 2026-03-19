# PRD - Accounts Control System
## Break 7.52 Palermo
## Target Architecture: ASP.NET Core Web API + Blazor Frontend

---

## 0. Document Metadata

- **Document Type:** Product Requirements Document
- **Product Name:** Accounts Control
- **Business Context:** Internal management system for Break 7.52 Palermo
- **Version:** 2.0
- **Date:** March 2026
- **Status:** Draft for migration / redesign
- **Target Stack:**
  - **Backend API:** ASP.NET Core Web API
  - **Frontend:** Blazor
  - **Database:** PostgreSQL
  - **ORM:** Entity Framework Core
  - **Authentication:** Role-based authentication for admin; lightweight access model for employee/expense access
  - **Hosting:** Cloud-hosted web application
- **Primary Goal:** Replace the original HTML + Supabase implementation with a maintainable, scalable architecture using .NET backend services and a Blazor frontend.

---

## 1. Executive Summary

The system manages internal employee debts and store-wide expense accounts for Break 7.52 Palermo. It replaces a prior spreadsheet-based workflow that allowed employees to modify debt values without traceability.

The new solution must provide:

1. **Immutable movement registration**
2. **Role separation**
3. **Auditability**
4. **Cloud persistence**
5. **Real-time or near-real-time updated balances**
6. **Admin-only adjustment capabilities**
7. **A .NET API backend and Blazor-based frontend**

---

## 2. Problem Statement

The previous process was based on Google Sheets and had the following issues:

- Employees could manually edit debt balances
- There was no reliable audit trail
- There was no enforced role separation
- The source of each modification was not recorded
- Errors and fraud could not be easily detected

The new system must ensure that:

- employees can only append new positive charges,
- administrators can apply corrections,
- all actions are persisted in a structured database,
- balances are derived from movement history rather than manually edited fields.

---

## 3. Product Goals

### 3.1 Primary Goals

- Prevent unauthorized balance manipulation
- Centralize account and movement management
- Support both employee debt accounts and general expense accounts
- Provide a secure administration workflow
- Expose business functionality through a .NET API
- Deliver the user experience through a Blazor application

### 3.2 Secondary Goals

- Make the system extensible for future modules
- Improve maintainability over the original HTML-only version
- Make the specification easy for an LLM or coding assistant to interpret

---

## 4. Architecture Overview

## 4.1 High-Level Architecture

### Frontend
- Blazor application
- Responsible for:
  - login selection flows
  - employee views
  - expense account views
  - admin views
  - form validation
  - navigation
  - rendering summaries and histories
  - calling backend APIs

### Backend
- ASP.NET Core Web API
- Responsible for:
  - authentication and authorization
  - account management
  - movement registration
  - adjustment logic
  - balance calculation
  - validation of business rules
  - auditability
  - database persistence

### Database
- PostgreSQL
- Stores:
  - accounts
  - movements
  - configuration
  - optional auth/session tables
  - optional audit log tables

---

## 5. Core Domain Concepts

## 5.1 Account

Represents either:

- an employee account
- a general expense account

Each account has a balance derived from its movements.

## 5.2 Movement

A financial event associated with an account.

Examples:
- employee debt charge
- general expense registration
- admin adjustment
- admin payment / discount

A movement is immutable after creation.

## 5.3 Balance

A calculated value based on all movements for an account.

Balance must **not** be stored as an editable field that users can manipulate directly.

## 5.4 Admin Configuration

Stores system-level settings such as:
- administrator PIN or credential configuration
- future settings

---

## 6. Roles and Permissions

## 6.1 Role: Employee

### Access Model
- Selects own account from a list, or uses a lightweight identity mechanism depending on final implementation

### Permissions
- View own current balance
- Register new debt movements with positive amounts only
- View own movement history

### Restrictions
- Cannot edit previous movements
- Cannot delete previous movements
- Cannot apply discounts or payments
- Cannot view other employee accounts
- Cannot access admin endpoints or admin UI

---

## 6.2 Role: GeneralExpenseUser

### Access Model
- Selects a general expense account from a dedicated list, or uses a lightweight identity mechanism depending on final implementation

### Permissions
- View balance of assigned expense account
- Register new expense movements with positive amounts only
- View movement history for assigned expense account

### Restrictions
- Cannot apply discounts
- Cannot apply payments
- Cannot view other accounts
- Cannot access admin endpoints or admin UI

---

## 6.3 Role: Administrator

### Access Model
- Authenticated access through secure admin authentication
- Recommended migration path: replace raw PIN-only access with proper authentication in API

### Permissions
- View all accounts
- View all movements
- Apply positive or negative adjustments
- Add accounts
- Remove accounts
- Change admin credential settings
- View global metrics
- Access all admin APIs and admin Blazor views

### Restrictions
- Cannot delete historical movements through normal UI unless explicitly supported in future versions
- Must use adjustment flows rather than mutating movement history

---

## 7. Functional Requirements

---

## FR-01 - Global Store Balance on Landing Screen

### Description
Before login, the application displays the total outstanding balance across all active accounts.

### Backend Responsibilities
- Provide an endpoint to return:
  - total outstanding balance
  - total general expenses
  - total collected if needed
- Calculate from persisted movement data

### Frontend Responsibilities
- Show total balance on landing page
- Use color coding:
  - red if greater than zero
  - green if zero

### Acceptance Criteria
- Visible before any user logs in
- Read-only
- Reflects current database state
- Updates when returning to landing screen after logout

---

## FR-02 - Role-Based Entry Paths

### Description
The landing screen exposes three access paths:

- employee
- general expense
- administrator

### Backend Responsibilities
- Provide account lists filtered by account type
- Validate admin authentication

### Frontend Responsibilities
- Show three clearly separated login/access panels
- Employee panel must only show employee accounts
- Expense panel must only show expense accounts
- Admin panel must accept secure credential input

### Acceptance Criteria
- Role paths are visually and functionally separated
- Incorrect admin credentials show a visible error
- Failed admin login does not break the session or UI state

---

## FR-03 - Employee Balance View

### Description
Employee users can view their own outstanding balance in a prominent read-only section.

### Backend Responsibilities
- Return account summary with calculated balance
- Return movement count and last activity if desired

### Frontend Responsibilities
- Show account name
- Show current balance
- Show read-only indicator text

### Acceptance Criteria
- User cannot edit the balance directly
- Balance is derived from movement history
- Balance updates immediately after successful debt registration

---

## FR-04 - Employee Debt Registration

### Description
Employees can create a new debt movement for their own account.

### Input Fields
- Description
- Amount
- Shift

### Validation Rules
- Description required
- Amount required
- Amount must be greater than zero
- Shift required
- No negative values allowed

### Backend Responsibilities
- Expose endpoint to create movement
- Enforce account ownership / access constraints
- Persist movement with metadata
- Set movement origin to employee
- Timestamp using server time

### Frontend Responsibilities
- Render form
- Validate before submit
- Show loading state
- Show success/failure feedback
- Clear form after success

### Movement Data Requirements
- AccountId
- Description
- Amount
- Shift
- MovementType = Charge
- CreatedAt
- CreatedByRole = Employee
- IsAdminAdjustment = false

### Acceptance Criteria
- Negative or zero values are rejected
- Successful creation updates visible balance
- Failed API requests do not mutate local state incorrectly
- Employee cannot create movements for another account

---

## FR-05 - General Expense Registration

### Description
General expense users can create a new positive expense movement for their own expense account.

### Input Fields
- Description
- Amount

### Validation Rules
- Description required
- Amount required
- Amount must be greater than zero

### Backend Responsibilities
- Expose create movement endpoint or specialized expense endpoint
- Persist movement with account type = expense context
- Save shift as null or empty according to schema decision

### Frontend Responsibilities
- Render expense registration form
- Use expense-specific labels and placeholders
- Hide shift field

### Acceptance Criteria
- Only positive values accepted
- Expense account holder can only register for own account
- Shift is omitted or persisted as null/empty consistently
- UI visually differentiates expense accounts from employee accounts

---

## FR-06 - Account Movement History

### Description
Employees and general expense users can view the full history for their own account in reverse chronological order.

### Data Per Item
- Description
- Timestamp
- Shift if applicable
- Movement type
- Badge
- Signed amount
- Whether it was admin-generated

### Backend Responsibilities
- Expose paged or unpaged account movement history endpoint
- Filter by account access permissions
- Order by timestamp descending

### Frontend Responsibilities
- Render read-only movement list
- Show badges:
  - charge
  - discount
  - admin

### Acceptance Criteria
- User only sees movements belonging to their account
- No edit or delete controls are shown
- Admin adjustments are visibly identified

---

## FR-07 - Restriction Notice

### Description
The registration view must show a persistent notice explaining that users can only add charges and must contact administration for discounts or corrections.

### Frontend Responsibilities
- Display fixed message below entry form
- Use highlighted visual style
- Show lock icon or equivalent

### Acceptance Criteria
- Always visible
- Cannot be dismissed
- Distinct from normal form content

---

## FR-08 - Admin Summary Dashboard

### Description
Administrator main dashboard showing global metrics and account lists.

### Required Metrics
- Total store balance
- Total general expenses
- Total collected from negative admin adjustments

### Account Lists
- Employees
- General expenses

### Row Data
- Account name
- Optional initials/avatar
- Number of movements
- Current balance

### Backend Responsibilities
- Expose summary endpoint returning:
  - global metrics
  - account summaries
  - movement counts
- Sort accounts by descending balance

### Frontend Responsibilities
- Render metric cards
- Render account sections
- Open detail modal or detail page on selection

### Acceptance Criteria
- Metrics update after adjustments
- Sorting by descending balance
- Dashboard reflects real database state

---

## FR-09 - Admin Account Detail and Adjustment Workflow

### Description
The administrator can open an account detail view and create positive or negative adjustments.

### Input Fields
- Concept / description
- Amount
- Adjustment direction:
  - positive charge
  - negative discount/payment

### Validation Rules
- Description required
- Amount required
- Amount must be greater than zero before sign transformation
- Admin selects direction explicitly

### Backend Responsibilities
- Expose endpoint to create admin adjustment
- Convert signed result based on adjustment type
- Mark movement as admin-generated
- Persist audit metadata

### Frontend Responsibilities
- Render account detail panel/page
- Render adjustment form
- Refresh history after success
- Refresh dashboard balances after success

### Movement Data Requirements
- AccountId
- Description
- AbsoluteAmount
- SignedAmount
- MovementType
- IsAdminAdjustment = true
- CreatedByRole = Administrator
- CreatedAt
- Optional AdminUserId

### Acceptance Criteria
- Only admin can access this workflow
- Negative adjustments are admin-only
- New movement appears immediately in history
- Updated balance appears in detail view and dashboard without full reload if possible

---

## FR-10 - Global Movement Log

### Description
Administrator can view all movements across the system in a single chronological view.

### Data Per Item
- Account name
- Account type
- Description
- Timestamp
- Shift if applicable
- Movement type
- Signed amount
- Origin role

### Backend Responsibilities
- Expose global movements endpoint
- Return reverse chronological results
- Optional filtering support for future versions

### Frontend Responsibilities
- Render global log as read-only list or table

### Acceptance Criteria
- Includes employee charges
- Includes expense entries
- Includes admin adjustments
- Read-only in version 1

---

## FR-11 - Account Management

### Description
Administrator can create and remove accounts without code changes.

### Account Types Supported
- Employee
- GeneralExpense

### Create Account Inputs
- Name
- Account type

### Validation Rules
- Name required
- Name must be unique case-insensitively
- Account type required

### Delete Account Rules
- Deleting an account must not delete historical movements
- Deletion policy should be either:
  - soft delete recommended
  - hard delete of account row only if historical referential strategy supports it

### Backend Responsibilities
- Expose create account endpoint
- Expose delete/deactivate account endpoint
- Enforce uniqueness
- Keep history intact

### Frontend Responsibilities
- Render account administration page
- Show create forms
- Show delete controls with confirmation

### Acceptance Criteria
- New accounts appear immediately in relevant UI lists
- Duplicate names are rejected
- Historical movements remain queryable after account deletion/deactivation

---

## FR-12 - Admin Credential Management

### Description
Administrator can update admin access credentials/settings.

### Recommended Design Note
For the .NET API architecture, a true admin authentication model is preferred over a plain stored PIN. If PIN compatibility is required initially, the PIN must be stored securely as a hash.

### Validation Rules
- Current credential must be validated
- New credential must meet minimum policy
- Audit trail should capture change event

### Backend Responsibilities
- Expose credential update endpoint
- Validate current admin authentication
- Persist securely

### Frontend Responsibilities
- Render settings form
- Display success/failure state

### Acceptance Criteria
- Incorrect current credential blocks update
- New credential must meet policy
- New credential becomes effective immediately

---

## FR-13 - Top Bar Store Balance

### Description
When a user is logged in, the top bar shows the application title and total store outstanding balance.

### Backend Responsibilities
- Provide current total balance in summary or dedicated endpoint

### Frontend Responsibilities
- Render sticky top bar
- Refresh value after relevant writes

### Acceptance Criteria
- Visible in employee and admin experience
- Updated without full page reload when possible

---

## FR-14 - Persistent Cloud Data Storage

### Description
All system data is stored in a cloud-hosted PostgreSQL database and accessed only through the backend API.

### Backend Responsibilities
- Own all data access
- Use EF Core migrations
- Prevent frontend direct DB access
- Enforce business rules server-side

### Frontend Responsibilities
- Never persist authoritative business state in browser local storage
- Use API as source of truth

### Acceptance Criteria
- Data visible from any authorized client
- Writes persist centrally
- Browser storage is not used as authoritative persistence

---

## 8. Data Model

## 8.1 Entity: Account

### Fields
- `Id`
- `Name`
- `Type`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`
- `DeletedAt` (optional for soft delete)

### Notes
- `Type` values:
  - `Employee`
  - `GeneralExpense`

---

## 8.2 Entity: Movement

### Fields
- `Id`
- `AccountId`
- `Description`
- `Amount`
- `Shift`
- `MovementType`
- `IsAdminAdjustment`
- `CreatedAt`
- `CreatedByRole`
- `CreatedByUserId` (optional)
- `MetadataJson` (optional)

### Notes
- `Amount` should be signed:
  - positive for charges / expenses
  - negative for discounts / payments
- `MovementType` examples:
  - `Charge`
  - `Expense`
  - `Discount`
  - `Payment`
  - `AdminAdjustment`

---

## 8.3 Entity: Configuration

### Fields
- `Id`
- `Key`
- `Value`
- `UpdatedAt`

### Use Cases
- Admin credential settings
- Future application settings

---

## 8.4 Optional Entity: AuditLog

### Fields
- `Id`
- `Action`
- `EntityType`
- `EntityId`
- `PerformedBy`
- `PerformedAt`
- `PayloadJson`

### Purpose
Recommended for traceability of admin actions beyond movement records.

---

## 9. API Design Requirements

## 9.1 API Style

- RESTful JSON API
- Versioned endpoints recommended:
  - `/api/v1/...`

---

## 9.2 Suggested Endpoint Groups

### Public / Shared Read Endpoints
- `GET /api/v1/app/summary`
- `GET /api/v1/accounts/employee`
- `GET /api/v1/accounts/expenses`

### Employee / Expense Access Endpoints
- `GET /api/v1/accounts/{accountId}/summary`
- `GET /api/v1/accounts/{accountId}/movements`
- `POST /api/v1/accounts/{accountId}/movements`

### Admin Endpoints
- `POST /api/v1/admin/auth/login`
- `POST /api/v1/admin/auth/change-credential`
- `GET /api/v1/admin/dashboard`
- `GET /api/v1/admin/accounts`
- `POST /api/v1/admin/accounts`
- `DELETE /api/v1/admin/accounts/{accountId}`
- `GET /api/v1/admin/accounts/{accountId}`
- `POST /api/v1/admin/accounts/{accountId}/adjustments`
- `GET /api/v1/admin/movements`

---

## 9.3 API Contract Rules

- All write operations must validate server-side
- All timestamps must come from the server
- All authorization decisions must be backend-enforced
- The API must not trust role claims coming only from the frontend
- Validation errors should return structured responses

### Recommended Error Response Shape
- `code`
- `message`
- `details`
- `traceId`

---

## 10. Blazor Frontend Requirements

## 10.1 Application Areas

### Public Area
- Landing page
- Global balance
- Employee selector
- Expense selector
- Admin login form

### Employee Area
- Account summary page
- New debt form
- My history page

### Expense Area
- Expense summary page
- New expense form
- History page

### Admin Area
- Dashboard
- Account detail page / modal
- Global movements page
- Account management page
- Settings page

---

## 10.2 Blazor UX Requirements

- Form validation should happen client-side and server-side
- Loading indicators required for all write operations
- Error messages should be visible and actionable
- UI should refresh summaries after writes
- The app should clearly distinguish employee vs expense vs admin views
- Components should be reusable where possible

---

## 11. Business Rules

- **BR-01:** Employees can only register amounts greater than zero
- **BR-02:** General expense users can only register amounts greater than zero
- **BR-03:** Employees cannot view or modify other accounts
- **BR-04:** General expense users cannot view or modify other accounts
- **BR-05:** Only administrators can apply negative adjustments
- **BR-06:** Historical movements are immutable after creation
- **BR-07:** Balances are derived from movement history
- **BR-08:** No two accounts may share the same name case-insensitively
- **BR-09:** Account deletion must not erase financial history
- **BR-10:** All timestamps come from the backend
- **BR-11:** Frontend validation is convenience only; backend validation is authoritative
- **BR-12:** Admin credential storage must be secure
- **BR-13:** The frontend must not directly access the database
- **BR-14:** The system must prevent negative values in employee and expense self-registration flows

---

## 12. Non-Functional Requirements

## 12.1 Security
- Admin authentication must be server-enforced
- Sensitive credentials must be hashed, not stored as plain text
- API endpoints must be authorization-protected
- Input validation must exist on all write paths

## 12.2 Reliability
- Failed writes must not corrupt UI state
- Database transactions should be used where appropriate

## 12.3 Maintainability
- Backend should use layered architecture or clean architecture principles
- Domain rules should be centralized in backend services
- DTOs should be separate from EF entities

## 12.4 Performance
- Dashboard and history queries should be indexed appropriately
- Global log may need pagination in later versions
- Balance calculations should be efficient even with growth

## 12.5 Auditability
- Movement creation must be timestamped
- Admin actions should be auditable
- Balance must be reproducible from history

---

## 13. Suggested Backend Project Structure

### Suggested Projects
- `AccountsControl.Api`
- `AccountsControl.Application`
- `AccountsControl.Domain`
- `AccountsControl.Infrastructure`

### Responsibilities
- **Api**
  - controllers / endpoints
  - authentication setup
  - dependency injection
- **Application**
  - use cases
  - commands
  - queries
  - validators
  - DTOs
- **Domain**
  - entities
  - enums
  - business rules
- **Infrastructure**
  - EF Core
  - repositories
  - migrations
  - persistence
  - external services

---

## 14. Suggested Blazor Project Structure

### Suggested Areas / Folders
- `Pages/Public`
- `Pages/Employee`
- `Pages/Expense`
- `Pages/Admin`
- `Components/Common`
- `Components/Forms`
- `Services/Api`
- `Models/ViewModels`
- `State`

### Notes
- UI components should separate rendering from API client logic
- Shared components may be used for:
  - balance cards
  - movement lists
  - loading overlays
  - toast notifications
  - validation summaries

---

## 15. User Flows

## 15.1 Employee Registers a Debt

1. User opens app
2. User sees global balance on landing page
3. User selects employee account
4. User enters employee area
5. User sees current read-only balance
6. User fills in description, amount, shift
7. User submits form
8. Frontend validates
9. Backend validates and persists movement
10. Success response returned
11. Frontend refreshes balance and history preview

---

## 15.2 General Expense User Registers an Expense

1. User opens app
2. User selects expense account
3. User enters expense area
4. User sees current account balance
5. User fills in description and amount
6. User submits form
7. Backend stores positive movement
8. Frontend refreshes balance and history

---

## 15.3 Admin Applies a Payment / Discount

1. Admin logs in
2. Admin opens dashboard
3. Admin selects account
4. Admin opens detail view
5. Admin enters concept and amount
6. Admin selects negative adjustment type
7. Admin submits adjustment
8. Backend persists signed negative movement
9. Frontend refreshes account history and dashboard metrics

---

## 15.4 Admin Adds a New Account

1. Admin opens account management page
2. Admin enters account name
3. Admin selects account type
4. Admin submits
5. Backend validates uniqueness and persists
6. Frontend refreshes account lists

---

## 16. System States

- **Loading**
  - App initialization in progress
  - API calls pending
  - UI interaction limited where appropriate

- **PublicLanding**
  - Global summary visible
  - Role entry paths visible

- **EmployeeAuthenticatedContext**
  - Employee summary and self-service form visible

- **ExpenseAuthenticatedContext**
  - Expense summary and self-service form visible

- **AdminAuthenticated**
  - Full dashboard and admin functions visible

- **Saving**
  - Write operation in progress
  - Duplicate submissions prevented

- **Error**
  - API or validation failure state shown to user

---

## 17. Migration Notes from Original Version

## 17.1 Original Version
- HTML + CSS + JavaScript frontend
- Supabase direct persistence
- Admin PIN in app-level config
- No formal API layer

## 17.2 New Version
- Blazor frontend
- ASP.NET Core Web API
- PostgreSQL via EF Core
- Better separation of concerns
- Server-side business rule enforcement
- Improved extensibility
- Stronger auth model recommended

---

## 18. Open Technical Decisions

These decisions should be finalized before implementation begins:

1. Will employee/expense access remain dropdown-based or become authenticated?
2. Will admin use PIN-only compatibility at first or full username/password/JWT authentication?
3. Will balances be calculated dynamically on every query or cached/materialized for performance?
4. Will account deletion be soft-delete only?
5. Will the global movement log be paginated in v1?
6. Will the UI use Blazor Server or Blazor WebAssembly?
7. Will real-time refresh use polling, manual refresh, or SignalR?

---

## 19. Recommended Implementation Defaults

If no explicit technical decision is made, use the following defaults:

- **Frontend model:** Blazor WebAssembly hosted by ASP.NET Core
- **Authentication:** JWT for admin; lightweight account selection for employee/expense in v1
- **Persistence:** PostgreSQL + EF Core
- **Deletion strategy:** soft delete accounts
- **Movement history:** immutable
- **Audit:** add audit log for admin actions
- **API versioning:** yes
- **Validation:** FluentValidation or equivalent backend validation layer
- **Real-time behavior:** refresh on successful write; SignalR optional for later
- **Credential storage:** hashed and salted

---

## 20. Roadmap / Future Modules

- Shift closing
- Product price list
- Supplier work orders
- Incorrect order tracking
- Salary advances
- Broken / expired merchandise
- Ice control
- Points system
- Deputy integration
- Monthly settlement
- PDF export

Each future module should follow the same architecture pattern:
- backend domain model
- API endpoints
- Blazor pages/components
- persistent storage
- explicit business rules

---

## 21. Implementation Notes for LLM Consumption

This document is intentionally structured for machine readability.

### Parsing Guidelines
- Requirement IDs are stable
- Business rules are centralized in section 11
- Domain entities are centralized in section 8
- API concerns are centralized in section 9
- Frontend concerns are centralized in section 10

### Important Constraints for Code Generation
- Do not store balance as an editable user field
- Do not allow employee-created negative movements
- Do not allow deleting historical movements in standard workflows
- Enforce authorization on backend, not only in Blazor UI
- Use server timestamps
- Keep history immutable
- Preserve auditability

---

## 22. Final Summary

This redesigned PRD defines the migration of the Break 7.52 Palermo account control system from a lightweight HTML + Supabase solution to a structured `.NET API + Blazor` architecture.

The key architectural principle is:

> **Balances are computed from immutable movements, and all authority is enforced by the backend API.**

This enables:
- traceability,
- maintainability,
- safer operations,
- future extensibility.

---
