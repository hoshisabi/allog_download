import csv
import json
import argparse
import os

class CharacterCSVParser:
    def __init__(self, character_id, csv_file, json_file, add_if_missing=False):
        """Initialize the parser with character ID, CSV file, and JSON file."""
        self.filename = csv_file
        self.json_file = json_file
        self.character_id = character_id
        self.add_if_missing = add_if_missing
        self.character_data = {"sessions": [], "magic_items": []}  # Ensure initialization

    def parse_csv(self):
        """Parse CSV and extract session details and magic items."""
        with open(self.filename, mode="r", encoding="utf-8") as file:
            reader = csv.reader(file)

            for row in reader:
                if not row or len(row) < 2:  # Ignore empty or malformed rows
                    continue

                # Process session log entries, but ignore header (Adventure Title is the second item in the header)
                if row[0] in ["CharacterLogEntry", "DmLogEntry"] and row[1] != "Adventure Title":
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

                # Process magic item entries, but ignore header (name is the second item in the header)
                elif row[0] == "MAGIC ITEM" and row[1] != "name":
                    self.character_data["magic_items"].append({
                        "name": row[1],
                        "rarity": row[2],
                        "location_found": row[3],
                        "table": row[4] if len(row) > 4 else None,
                        "table_result": row[5] if len(row) > 5 else None,
                        "notes": row[6] if len(row) > 6 else None
                    })

    def update_json(self):
        """Updates characters.json with new character data or modifies existing entry."""
        if os.path.exists(self.json_file):
            with open(self.json_file, "r", encoding="utf-8") as file:
                try:
                    all_characters = json.load(file)
                except json.JSONDecodeError:
                    all_characters = {}
        else:
            all_characters = {}

        # Ensure the character ID is included in the data
        self.character_data["id"] = self.character_id

        if self.character_id in all_characters:
            print(f"Updating existing character [id: {self.character_id}]")
            all_characters[self.character_id].update(self.character_data)
        elif self.add_if_missing:
            print(f"Adding new character [id: {self.character_id}]")
            all_characters[self.character_id] = self.character_data
        else:
            print(f"Character with ID {self.character_id} does not exist in JSON file. Download the updated list, or use --add if you wish to force download of sessions.")
            return  # Exit if not adding and character already exists   

        with open(self.json_file, "w", encoding="utf-8") as file:
            json.dump(all_characters, file, indent=4)
        print(f"Data successfully written to {self.json_file}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Parse character CSV file and update characters.json.")
    parser.add_argument("-c", "--csv", required=True, type=str, help="Path to the individual character CSV file")
    parser.add_argument("-j", "--json", required=True, type=str, help="Path to the characters JSON file")
    parser.add_argument("-i", "--id", required=True, type=str, help="Character id to parse")
    parser.add_argument("-a", "--add", action="store_true", help="Add character to JSON file if not already present")

    args = parser.parse_args()

    csv_parser = CharacterCSVParser(args.id, args.csv, args.json, args.add)
    csv_parser.parse_csv()
    csv_parser.update_json()

