# WorkAt

A web-based job portal system that connects companies with job seekers through online job posting, job searching, and application management.

## Technology Stack

* ASP.NET Core MVC
* C#
* Entity Framework Core
* ASP.NET Core Identity
* SQL Server Express LocalDB
* Bootstrap
* jQuery

## Main Modules

* User Registration and Authentication
* Company Management
* Job Seeker Management
* Job Posting and Management
* Job Search and Filtering
* Resume and Skill Management
* Job Application
* Application Status and Feedback
* Administrator Management

## Main Features

* Role-based access for Admin, Company, and JobSeeker
* Company and Job Seeker verification
* Job posting and CRUD operations
* Job search and filtering
* Resume and skill management
* Online job application
* Application status tracking
* Company feedback on applications
* Authorized resume viewing and downloading
* Administrator management of users and applications

## Database

WorkAt uses SQL Server Express LocalDB with Entity Framework Core Code First.

Main application entities include:

* Admin
* Company
* JobSeeker
* Job
* Application
* ApplicationFeedback
* Resume
* Skill
* ResumeSkill

## Project Structure

```text
WorkAt/
├── Controllers/
├── Models/
├── Views/
├── Data/
├── Migrations/
└── wwwroot/
```
