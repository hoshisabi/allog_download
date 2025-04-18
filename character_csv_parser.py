import csv
import json
import argparse
import os

class CharacterCSVParser:
    def __init__(self, filename, json_file="characters.json"):
        self.filename = filename
        self.json_file = json_file
        self.character_data = {"sessions": [], "magic_items": []}  # Ensure initialization

    def parse_csv(self):
        """Parse CSV and extract character details, sessions, and magic items."""
        with open(self.filename, mode="r", encoding="utf-8") as file:
            reader = csv.reader(file)

            for row in reader:
                if not row or len(row) < 2:  # Ignore empty or malformed rows
                    continue

                if row[0] == "name" and len(row) >= 7:  # Adjusted index range
                    self.character_data.update({
                        "name": row[1],
                        "race": row[2],
                        "class": row[3],
                        "background": row[4],  # Keeping background, but removing faction & lifestyle
                        "portrait_url": row[5] if row[5] else None,
                        "publicly_visible": row[6].lower() == "true",
                        "sessions": [],
                        "magic_items": []
                    })

                elif row[0] in ["CharacterLogEntry", "DmLogEntry"]:
                    if "sessions" not in self.character_data:
                        self.character_data["sessions"] = []  # Ensure sessions exists

                    self.character_data["sessions"].append({
                        "type": row[0],
                        "adventure_title": row[1],
                        "session_number": row[2] if row[2] else None,
                        "date_played": row[3] if row[3] else None,
                        "session_length_hours": row[4] if row[4] else None,
                        "player_level": row[5] if row[5] else None,
                        "xp_gained": float(row[6]) if row[6] else None,
                        "gp_gained": float(row[7]) if row[7] else None,
                        "downtime_gained": float(row[8]) if row[8] else None,
                        "renown_gained": float(row[9]) if row[9] else None,
                        "location_played": row[11] if len(row) > 11 else None,
                        "dm_name": row[12] if len(row) > 12 else None,
                        "notes": row[14] if len(row) > 14 else None
                    })

                elif row[0] == "MAGIC ITEM" and len(row) >= 4:  # Ensure proper magic item structure
                    magic_item = {
                        "name": row[1],
                        "rarity": row[2],
                        "location_found": row[3]
                    }
                    if magic_item["name"].lower() != "name":  # Filter placeholder entry
                        self.character_data["magic_items"].append(magic_item)

    def update_json(self):
        """Updates characters.json with new character data or modifies existing entry."""
        if "name" not in self.character_data:
            print("Error: Missing 'name' field in character data. Parsing may have failed.")
            return  # Exit gracefully instead of crashing

        if os.path.exists(self.json_file):
            with open(self.json_file, "r", encoding="utf-8") as file:
                try:
                    all_characters = json.load(file)
                except json.JSONDecodeError:
                    all_characters = []

        else:
            all_characters = []

        existing_character = next((char for char in all_characters if char.get("name") == self.character_data["name"]), None)

        if existing_character:
            print(f"Updating existing character: {self.character_data['name']}")
            existing_character.update(self.character_data)
        else:
            print(f"Adding new character: {self.character_data['name']}")
            all_characters.append(self.character_data)

        with open(self.json_file, "w", encoding="utf-8") as file:
            json.dump(all_characters, file, indent=4)
        print(f"Data successfully written to {self.json_file}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Parse character CSV file and update characters.json.")
    parser.add_argument("filename", help="Path to the CSV file")

    args = parser.parse_args()

    csv_parser = CharacterCSVParser(args.filename)
    csv_parser.parse_csv()
    csv_parser.update_json()

