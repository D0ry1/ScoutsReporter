# Scouts Reporter

A Windows desktop application for Scout leaders, commissioners and group administrators to quickly check the compliance status of their volunteers. It connects to the Scouts membership system and generates reports on DBS checks, training, and activity permits - all in one place.

## [Download the latest version here](https://github.com/D0ry1/ScoutsReporter/releases/latest)

1. Click the link above and download **ScoutsReporter-v1.0.0.zip**
2. Extract the zip to any folder on your computer (e.g. Desktop or Documents)
3. Double-click **ScoutsReporter.exe** to run it

**Requires:** Windows 10 or 11 with the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0/runtime) installed. If you don't have it, Windows will prompt you to download it when you first run the app.

---

## What does it do?

Scouts Reporter pulls data from the same system that powers the Scouts membership website and presents it in a simple, searchable table format. Instead of clicking through dozens of individual profiles online, you can see everyone's status at a glance.

It produces three reports:

| Report | What it shows |
|---|---|
| **DBS / Disclosures** | Whether each volunteer has a valid DBS check, when it expires, and any outstanding onboarding actions |
| **Training** | Completion status of mandatory and optional training modules (First Response, Safeguarding, Safety, etc.) with expiry warnings |
| **Permits** | Activity permits held by each volunteer, their status and expiry dates |

Each report colour-codes members so you can immediately spot who needs attention:
- **Green (OK)** - everything is in order
- **Yellow (Expiring Soon / In Progress)** - action needed soon
- **Red (Expired / Missing / Action Needed)** - urgent attention required
- **Blue (Not in System / No Permits)** - informational

---

## Getting started

### What you need

- A Windows 10 or Windows 11 computer
- An internet connection
- A Scouts membership account with access to at least one unit (the same login you use for the Scouts membership website)

### Installing

1. Download the latest release (the folder containing `ScoutsReporter.exe`)
2. Place it anywhere on your computer - your Desktop, Documents folder, or wherever is convenient
3. Double-click `ScoutsReporter.exe` to run it

There is nothing else to install. If Windows SmartScreen shows a warning the first time you run it, click **"More info"** then **"Run anyway"** - this is normal for new applications.

### First time setup

No configuration is needed. The app will ask you to log in and that's it.

---

## How to use it

### Logging in

1. Open the app. You will see the purple header bar with a **Login** button on the right.
2. Click **Login**. A browser window will open showing the Scouts login page.
3. Enter your Scouts membership email and password as you normally would.
4. Once you have logged in successfully, the browser window will close automatically and the app will show your name and group in the header.

Your login is remembered between sessions. The next time you open the app, it will try to log you in automatically. If that fails (for example, if it has been a long time), you will just need to click Login again.

### Running a report

1. Make sure you are logged in (your name should appear in the purple header bar).
2. Click on the tab for the report you want: **DBS / Disclosures**, **Training**, or **Permits**.
3. Click the **Run Report** button in the top-left of the tab.
4. Wait for the report to finish. You will see progress messages at the bottom of the screen telling you what is happening. The first report takes the longest (typically 30 seconds to a couple of minutes depending on how many volunteers you have) because it needs to fetch everyone's data. Subsequent reports are faster because member data is cached.

You can also click **Run All Reports** in the header bar to run all three reports one after another.

### Understanding the results

Each report displays a table of your volunteers. The key column to look at is the **Flag** column, which gives a quick status summary for each person.

**DBS / Disclosures report columns:**
- **Name** - the volunteer's display name
- **Mem #** - membership number
- **Email** - contact email address
- **Issued / Expiry** - when their DBS was issued and when it expires
- **Warning** - how long until expiry (e.g. "Expires in 3 months")
- **Onboarding DBS** - status of any in-progress DBS application
- **Disclosure Status** - current disclosure status from the membership system
- **Certificate / Type / Authority** - DBS certificate details
- **Flag** - overall status (OK, EXPIRED, EXPIRING SOON, ACTION NEEDED, etc.)
- **Outstanding** - any other outstanding onboarding actions
- **Roles** - all roles held by this person

**Training report columns:**
- **Name** - the volunteer's display name
- **Total Trainings** - number of completed training modules
- One column per training module showing the completion date
- Warning columns for First Response, Safeguarding and Safety showing time until expiry
- **Flag** - overall status
- **Roles** - all roles held

**Permits report columns:**
- **Name** - the volunteer's display name
- **Total Permits** - number of permits held
- **Flag** - overall status
- **Roles** - all roles held
- Click on a row to expand it and see the individual permit details (name, type, status, dates, restrictions)

### Searching and filtering

Each report tab has a **Search** box in the toolbar. Type a name, role, flag status, or any other text to filter the table to matching rows. Clear the box to show all rows again.

The Permits report also has a **"Hide members with no permits"** checkbox to focus on just those who hold permits.

### Exporting your data

Once a report has been generated, you can save it to a file:

- **Export CSV** - saves as a CSV file that can be opened in Excel, Google Sheets, or any spreadsheet application
- **Export Excel** - saves as a proper Excel (.xlsx) file with formatting

Click the button, choose where to save the file, and you're done. The default filename includes today's date and time so you can keep a history of reports.

### Selecting which units to include (multi-unit users)

If your Scouts role gives you access to more than one unit (for example, district or county commissioners), you will see a small **down arrow** next to the group name in the purple header bar.

1. Click the **down arrow** to open the unit picker.
2. Tick or untick the units you want to include in your reports.
3. Use the **All** or **None** buttons to quickly select or deselect everything.
4. The header will show how many units you have selected (e.g. "3 of 12 units selected").
5. When you change your selection, any previously run reports are cleared and will need to be run again with the new selection.

If you only have access to one unit, this feature is hidden and everything works as normal.

### Cancelling a report

If a report is taking too long or you started it by mistake, click the **Cancel** button that appears while a report is running.

### Logging out

Click the red **Logout** button in the header bar. This clears all data from the app and removes your saved login. You will need to log in again next time.

---

## Troubleshooting

### "Saved token expired. Please log in."
This is normal. Your login session lasts about 24 hours. Just click Login to sign in again.

### A report seems to get stuck
The Scouts membership API can sometimes be slow, especially for large groups or during busy periods. Give it a minute or two. If it genuinely hangs, click Cancel and try again.

### "No units selected" error
If you have multiple units, make sure at least one unit is ticked in the unit picker (click the down arrow next to the group name).

### The data doesn't look right
The app pulls live data from the Scouts membership system. If something looks wrong, check the member's profile on the membership website to compare. The data in the app should match what is shown online.

### Windows SmartScreen warning
Because the app is not published through the Microsoft Store, Windows may warn you the first time you run it. Click **"More info"** then **"Run anyway"**. This only happens once.

### The login window is blank or doesn't load
The app uses Microsoft Edge WebView2 to show the login page. This is built into Windows 10 and 11, but if it is missing for some reason, you can download it from Microsoft: search for "WebView2 Runtime download" and install it.

---

## Privacy and security

- The app connects **only** to the official Scouts membership API (the same system used by the membership website).
- Your login credentials are entered directly into the Scouts login page - they are never seen or stored by this app.
- A refresh token is saved locally on your computer (in the same folder as the app) so you don't have to log in every time. This token is automatically refreshed and only works for your account.
- No data is sent anywhere other than the official Scouts servers. The app does not collect analytics, phone home, or share any information with third parties.
- Report data exists only in memory while the app is running. When you close the app, it is gone (unless you have exported it to a file).

---

## For developers

### Tech stack
- C# / .NET 8
- WPF (Windows Presentation Foundation)
- CommunityToolkit.Mvvm (MVVM pattern with source generators)
- Microsoft.Web.WebView2 (embedded browser for login)
- ClosedXML (Excel export)

### Building from source

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Clone the repository and open a terminal in the `ScoutsReporter` folder (the project root)
3. Run:
   ```
   dotnet build
   ```
4. To run:
   ```
   dotnet run
   ```

### Project structure

```
ScoutsReporter/
  Converters/
    FlagToColorConverter.cs   - Status flag colour coding
  Fonts/
    NunitoSans.ttf            - Nunito Sans regular font
    NunitoSans-Italic.ttf     - Nunito Sans italic font
  Models/
    Member.cs                 - Member, UnitInfo, TeamInfo data classes
    SelectableUnit.cs         - Observable wrapper for unit picker
    DisclosureRecord.cs       - DBS disclosure data
    TrainingRecord.cs         - Training completion data
    PermitRecord.cs           - Activity permit data
  Services/
    AuthService.cs            - OAuth login, token refresh
    ApiService.cs             - All Scouts API calls
    DataCacheService.cs       - Shared cache for units/teams/members
    DisclosureService.cs      - DBS-specific business logic
    TrainingService.cs        - Training-specific business logic
    PermitService.cs          - Permits-specific business logic
    CsvExportService.cs       - CSV file export
    ExcelExportService.cs     - Excel file export
  ViewModels/
    MainViewModel.cs          - App shell, login/logout, unit picker
    DbsReportViewModel.cs     - DBS report logic
    TrainingReportViewModel.cs - Training report logic
    PermitsReportViewModel.cs - Permits report logic
  Views/
    LoginWindow.xaml          - WebView2 login browser
    DbsReportView.xaml        - DBS report tab
    TrainingReportView.xaml   - Training report tab
    PermitsReportView.xaml    - Permits report tab
  App.xaml                    - Global styles and resources
  MainWindow.xaml             - Main app window layout
  ScoutsReporter.csproj       - Project file
  app.ico                     - Application icon
```
