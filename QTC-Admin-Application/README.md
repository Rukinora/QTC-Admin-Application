# Leidos QTC Health Services - Workflow Queue (WFQ) Administrator Application

## Overview
This repository contains the Workflow Queue (WFQ) Administrator Application, a secure, enterprise-grade web interface developed for Leidos QTC Health Services.

Currently, Leidos QTC processes over 4 million pages of medical records daily, requiring administrators to rely on manual SQL scripts to configure workflows. This ASP.NET Core MVC application replaces those manual database interactions with a secure, GUI-based tool designed to enforce HIPAA/HITECH compliance, improve operational efficiency, and eliminate the risks associated with raw database mutations.

## Technical Architecture
The application is built using a modern 3-tier architecture with a strict "Database-First" integration approach to protect legacy data constraints.

* **Framework:** ASP.NET Core 8 MVC
* **Data Access:** Entity Framework Core (EF Core)
* **Database:** SQL Server 2022
* **Frontend:** Razor Views, HTML5/CSS3, and heavily customized Bootstrap 5 for Section 508 accessibility compliance.
* **Document Generation:** QuestPDF (for automated compliance reporting).

## Key Features
* **Safe Configuration:** Administrators can dynamically create, edit, and sequence workflows and workflow steps through the UI without writing SQL.
* **Task Intervention:** Provides the ability to view active task counts, unassign, reassign, or safely complete stuck medical tasks.
* **Transactional Integrity:** All multi-step administrative operations (e.g., safe-deletes with dependency checks) are wrapped in atomic database transactions to ensure partial data is never committed.
* **Compliance & Auditing:** Enforces strict authorization and generates automated, time-stamped CSV and PDF compliance reports.

## Local Setup & Installation

### Prerequisites
* .NET 8.0 SDK
* Visual Studio 2022 (or Rider / VS Code)
* SQL Server 2022 Developer Edition: The local database engine.
* SQL Server Management Studio (SSMS): The tool used to interact with the database.
* IIS enabled with ASP.NET core hosting bundle (for IIS deployment).

### Database Setup (Local Environment)
* Open SSMS and connect to your local server (ensure you check "Trust server certificate" if using local auth).
* Go to File > Open > File and open the `01_UAT_DB-Accounts-Permissions.sql` script. Execute it to create the empty Workflow database and configure initial permissions.
* Open and execute the `02_Schema.sql` script to build all the necessary tables, stored procedures, and views inside the newly created Workflow database.

### Configuration
1. Clone the repository to your local machine.
2. Open `QTC-Admin-Application.sln` in Visual Studio.
3. Locate the `appsettings.json` or `appsettings.Development.json` file.
4. Update the `ConnectionStrings` block to point to your local or remote SQL Server instance hosting the WFQ database.

## Team & Acknowledgements
This application was developed as a Senior Design Capstone Project (Fall 2025 - Spring 2026).

* **Project Advisor:** Huiping Guo
* **Sponsor Contacts:** Edmundo Guzman-Meza, Denise Tabilas
* **Development Team:** Samuel Acevedo, Alexander Diaz, Ruben Flores, Dylan Huang, Haik Oganesyan, Jesus Ojeda, Slok Patel, Edwin Rojas, Laila Velasquez, Zhen Zhao
