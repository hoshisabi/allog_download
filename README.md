# allog_download

A collection of Python scripts designed to download and process data from AdventurersLeagueLog.com. This project facilitates the extraction of character and session data, enabling local storage and generation of reports (e.g., PDFs).

## Features

*   **Character Data Download:** Scripts to download character information, likely in CSV format.
*   **CSV Parsing:** Tools to parse downloaded CSV files and convert them into structured data (e.g., JSON).
*   **PDF Generation:** Ability to generate character reports in PDF format from processed JSON data.

## Setup and Installation

This project requires Python 3.8+ and uses `pipenv` for dependency management.

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/allog_download.git
cd allog_download
```

### 2. Install Dependencies

It is highly recommended to use `pipenv` to manage project dependencies in a virtual environment.

```bash
pip install pipenv
pipenv install
pipenv shell
```

If you prefer `pip` and `virtualenv`:

```bash
python -m venv .venv
.venv\Scripts\activate   # On Windows
source .venv/bin/activate # On macOS/Linux
pip install -r requirements.txt
```

### 3. Download Required Fonts

For PDF generation, the `DejaVuSans.ttf` font is required.

1.  Download `DejaVuSans.ttf` from [DejaVu Fonts](https://dejavu-fonts.github.io/).
2.  Place the `DejaVuSans.ttf` file in the `src/` directory.

## Usage

The `src/` directory contains the main scripts. Here are some common use cases:

*   **Generate PDF from JSON:**
    ```bash
    python src/characters_json_to_pdf.py -j <input_json_file> -o <output_pdf_file>
    ```
*   **Parse Character CSV:**
    ```bash
    python src/character_csv_parser.py -c <input_csv_file> -j <output_json_file> -i <character_id>
    ```
*   **Download All CSVs:**
    ```bash
    python src/download_all_csv.py
    ```

Refer to individual script help (`python script_name.py --help`) for more options.

## Project Structure

```
allog_download/
├── .env                    # Environment variables
├── .gitignore              # Git ignore file
├── Pipfile                 # Pipenv dependency definition
├── Pipfile.lock            # Pipenv locked dependencies
├── README.md               # Project README
├── requirements.txt        # Pip dependencies (for pip users)
├── src/                    # Main source code directory
│   ├── auth.py             # Authentication related scripts
│   ├── character_csv_parser.py # Parses character CSVs
│   ├── character_list.py   # Manages character lists
│   ├── characters_json_to_pdf.py # Generates PDFs from character JSON
│   ├── csv_download.py     # Handles CSV downloading
│   ├── dmsession_list.py   # Manages DM session lists
│   └── ...                 # Other utility scripts
├── build/                  # Build artifacts (e.g., compiled scripts)
├── dist/                   # Distribution files
├── maintaindb/             # Database maintenance scripts/data
├── out/                    # Output files (e.g., generated PDFs, JSONs)
└── .venv/                  # Python virtual environment (if using virtualenv)
```

## Troubleshooting

*   **`ModuleNotFoundError`**: Ensure all dependencies are installed using `pipenv install` or `pip install -r requirements.txt` within your activated virtual environment.
*   **Font Issues**: Verify `DejaVuSans.ttf` is correctly placed in the `src/` directory.

---


## C# WPF App (Adventure League Log Downloader)

A modern WPF application targeting .NET 9.0 that ports the original Python functionality to a standalone Windows application.

### Features
- Native Windows authentication via Credential Manager.
- Scrapes character lists and logs directly from AdventurersLeagueLog.com.
- Export to JSON (and more formats coming soon).

### Installation & Distribution

#### From GitHub Releases (recommended)

Official builds are published on **[Releases](https://github.com/hoshisabi/allog_download/releases)** (for example **[v0.1.0](https://github.com/hoshisabi/allog_download/releases/tag/v0.1.0)**). Here is what to do on that page:

1. **Pick a download** under *Assets* (ignore “Source code” unless you are a developer):
   - **`AdventurersLeagueLogDownloader-v0.1.0-win-x64-selfcontained.zip`** (name includes the version tag) — **larger file, easiest choice.** Includes the .NET runtime. Use this if you are not sure.
   - **`AdventurersLeagueLogDownloader-v0.1.0-win-x64-framework.zip`** — **smaller file.** You must install the **[.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)** for Windows x64 first, then use this build.

2. **Requirements:** **Windows 64-bit** only. The app does not run on macOS, Linux, or 32-bit Windows.

3. **Install:** Download the ZIP, extract it to a folder you keep (for example `Documents\AllogDownloader`), and double-click **`Adventure League Log Downloader.exe`**.

4. **First run:** Sign in with your AdventurersLeagueLog.com account when prompted, choose a **data folder** under **Options → Data location**, then use the menus to refresh your character list, download CSVs, or open the extra windows for DM sessions and other site data.

Newer versions appear on the same Releases page; the **[Latest release](https://github.com/hoshisabi/allog_download/releases/latest)** link always points to the newest build with the same two ZIP choices (the version in the file names matches the tag).

#### Sharing a copy without GitHub

If you already have a published ZIP from Releases or from `dotnet publish`, zip the folder and share it. Recipients still need **Windows x64**; match the **self-contained** vs **framework** requirements above.

#### Building and Publishing
To create a fresh single-file executable for distribution:

```powershell
dotnet publish "Adventure League Log Downloader" -p:PublishProfile=FolderProfile
```

The output will be located in:
`Adventure League Log Downloader\bin\Release\net9.0-windows\publish\win-x64\Adventure League Log Downloader.exe`

### Development
- Open `Adventure League Log Downloader.sln` in Visual Studio 2022 or JetBrains Rider.
- Target Framework: `.NET 9.0-windows`.
- Requires `HtmlAgilityPack` and `CredentialManagement` NuGet packages.

---

## Legacy Python Utilities
