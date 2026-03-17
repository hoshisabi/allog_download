from fpdf import FPDF
import json
import argparse
import os

class CharacterPDF(FPDF):
    def header(self):
        self.set_font("DejaVuSans", "B", 12)
        self.cell(0, 10, "Character Report", border=0, ln=1, align="C")
        self.ln(10)

    def add_character(self, character_id, character_data):
        # Start a new page for each character
        self.add_page()

        # Add character ID and basic information in a compact/tabular format
        self.set_font("DejaVuSans", "B", 10)
        self.cell(0, 10, f"Character ID: {character_id}", ln=1)
        self.set_font("DejaVuSans", "", 10)

        # Create a table-like structure for character details
        details = [
            ("Name", character_data.get("name", "N/A")),
            ("Race", character_data.get("race", "N/A")),
            ("Class", character_data.get("class", "N/A")),
            ("Level", character_data.get("level", "N/A")),
            ("Season", character_data.get("season", "N/A")),
            ("Tag", character_data.get("tag", "N/A")),
        ]
        for label, value in details:
            self.cell(50, 8, f"{label}:", border=0)
            self.cell(0, 8, value, border=0, ln=1)

        # Add magic items section
        if "magic_items" in character_data and character_data["magic_items"]:
            self.ln(5)
            self.set_font("DejaVuSans", "B", 10)
            self.cell(0, 10, "Magic Items:", ln=1)
            self.set_font("DejaVuSans", "", 10)
            for item in character_data["magic_items"]:
                self.multi_cell(0, 8, f"- {item['name']} ({item['rarity']}) - Found at: {item.get('location_found', 'Unknown')}")

        # Add sessions section
        if "sessions" in character_data and character_data["sessions"]:
            self.ln(5)
            self.set_font("DejaVuSans", "B", 10)
            self.cell(0, 10, "Sessions:", ln=1)
            self.set_font("DejaVuSans", "", 10)
            for session in character_data["sessions"]:
                self.multi_cell(0, 8, f"- {session['adventure_title']} ({session['date_played']})\n"
                                      f"  XP: {session.get('xp_gained', 'N/A')}, GP: {session.get('gp_gained', 'N/A')}, "
                                      f"Downtime: {session.get('downtime_gained', 'N/A')}\n"
                                      f"  DM: {session.get('dm_name', 'N/A')} - Notes: {session.get('notes', 'N/A')}")
        self.ln(10)  # Add some space before the next character

def generate_pdf(json_file, output_pdf):
    # Load the JSON data
    with open(json_file, "r", encoding="utf-8") as file:
        characters = json.load(file)

    # Create the PDF
    pdf = CharacterPDF()
    pdf.set_auto_page_break(auto=True, margin=15)

    # Get the absolute path to the font files
    script_dir = os.path.dirname(os.path.abspath(__file__))
    font_path = os.path.join(script_dir, "DejaVuSans.ttf")
    font_path_bold = os.path.join(script_dir, "DejaVuSans-Bold.ttf")

    # Use a Unicode-compatible font
    pdf.add_font("DejaVuSans", "", font_path, uni=True)
    pdf.add_font("DejaVuSans", "B", font_path_bold, uni=True)
    pdf.set_font("DejaVuSans", "", 10)

    # Add each character to the PDF
    for character_id, character_data in characters.items():
        pdf.add_character(character_id, character_data)

    # Save the PDF
    pdf.output(output_pdf)
    print(f"PDF successfully created: {output_pdf}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate a PDF report from a characters JSON file.")
    parser.add_argument("-j", "--json", required=True, type=str, help="Path to the input JSON file")
    parser.add_argument("-o", "--output", required=True, type=str, help="Path to the output PDF file")

    args = parser.parse_args()

    generate_pdf(args.json, args.output)