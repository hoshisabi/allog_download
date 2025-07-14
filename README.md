# allog_download
A set of files to download your data from AdventurersLeagueLog.com

## **How to Install and Run the Scripts**

### **1. Install Python**

All of the scripts in this package are written in python, which many machines may already have installed. If you're on Windows, you might have to install it. Follow the next few steps to 

1. **Download Python**:
   - Go to the [official Python website](https://www.python.org/downloads/).
   - Download the latest version of Python (3.10 or later is recommended).

2. **Install Python**:
   - During installation, **check the box** that says **"Add Python to PATH"**.
   - Select the option to install Python for all users.
   - Complete the installation process.

3. **Verify Installation**:
   - Open a terminal (Command Prompt or PowerShell on Windows).
   - Run the following command to verify Python is installed:
     ```powershell
     python --version
     ```
   - You should see the installed Python version (e.g., `Python 3.10.x`).

---

### **2. Install pip (Python Package Manager)**
- `pip` is usually installed with Python by default. To verify:
  ```powershell
  pip --version
  ```
- If `pip` is not installed, follow the instructions [here](https://pip.pypa.io/en/stable/installation/).

---

### **3. Install Virtualenv (Optional but Recommended)**
A virtual environment isolates your Python dependencies, ensuring they don’t interfere with other projects.

1. **Install `virtualenv`**:
   ```powershell
   pip install virtualenv
   ```

2. **Create a Virtual Environment**:
   - Navigate to the folder where the scripts are located:
     ```powershell
     cd path\to\your\scripts
     ```
   - Create a virtual environment:
     ```powershell
     python -m virtualenv venv
     ```

3. **Activate the Virtual Environment**:
   - On Windows:
     ```powershell
     venv\Scripts\activate
     ```
   - On macOS/Linux:
     ```bash
     source venv/bin/activate
     ```

4. **Verify Activation**:
   - Your terminal prompt should now show `(venv)` at the beginning.

---

### **4. Install Required Libraries**
The scripts depend on several Python libraries. Install them using `pip`.

1. **Navigate to the Script Directory**:
   ```powershell
   cd path\to\your\scripts
   ```

2. **Install Dependencies**:
   - Run the following command to install the required libraries:
     ```powershell
     pip install fpdf beautifulsoup4 requests
     ```

3. **Verify Installation**:
   - Run the following command to check installed libraries:
     ```powershell
     pip list
     ```

---

### **5. Download Required Fonts**
For Unicode support in the PDF generation script, you need the `DejaVuSans.ttf` font.

1. **Download the Font**:
   - Download `DejaVuSans.ttf` from [DejaVu Fonts](https://dejavu-fonts.github.io/).

2. **Place the Font**:
   - Save the `DejaVuSans.ttf` file in the same directory as the scripts.

---

### **6. Run the Scripts**
1. **Prepare Input Files**:
   - Ensure you have the required input files (e.g., `characters.json` or CSV files) in the appropriate format.

2. **Run the Scripts**:
   - Example: Generate a PDF from `characters.json`:
     ```bash
     python characters_json_to_pdf.py -j characters.json -o characters_report.pdf
     ```
   - Example: Parse a CSV file and update `characters.json`:
     ```bash
     python character_csv_parser.py -c character_24436.csv -j characters.json -i 24436
     ```

3. **Check the Output**:
   - The output files (e.g., `characters_report.pdf`) will be saved in the specified location.

---

### **7. Troubleshooting**
- **Python Not Found**:
  - Ensure Python is added to your system PATH during installation.
  - Restart your terminal and try again.

- **Missing Libraries**:
  - If you see an error like `ModuleNotFoundError`, ensure you’ve installed the required libraries using `pip`.

- **Font Not Found**:
  - Ensure `DejaVuSans.ttf` is in the same directory as the script or update the script to point to the correct path.

---

### **8. Optional: Share the Scripts**
If you’re sharing these scripts with others:
1. **Include a `requirements.txt` File**:
   - Create a `requirements.txt` file with the following content:
     ```
     fpdf
     beautifulsoup4
     requests
     ```
   - Users can install all dependencies with:
     ```bash
     pip install -r requirements.txt
     ```

2. **Provide Clear Instructions**:
   - Share this guide or create a `README.md` file with similar instructions.

---

### **9. Example Directory Structure**
Here’s an example of how your project directory might look:
```
allog_download/
├── src/
│   ├── character_csv_parser.py
│   ├── characters_json_to_pdf.py
│   ├── csv_download.py
│   ├── character_list.py
│   └── DejaVuSans.ttf
├── out/
│   ├── characters.json
│   ├── character_24436.csv
│   └── characters_report.pdf
└── README.md
```

---

### **10. Next Steps**
- Test the scripts with your data.
- Share the scripts and instructions with others.
- Gather feedback and refine the scripts as needed.

Let me know if you’d like help creating a `README.md` file or packaging the scripts for easier distribution! 😊---

### **10. Next Steps**
- Test the scripts with your data.
- Share the scripts and instructions with others.
- Gather feedback and refine the scripts as needed.

Let me know if you’d like help creating a `README.md` file or packaging the scripts for easier distribution! 😊