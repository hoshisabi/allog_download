import os
import glob
import argparse
from character_csv_parser import CharacterCSVParser

def process_csv_files(directory, glob_pattern, json_file):
    """Iterate over CSV files and process them with CharacterCSVParser."""
    # Construct the full glob pattern
    pattern = os.path.join(directory, glob_pattern)
    csv_files = glob.glob(pattern)

    if not csv_files:
        print(f"No CSV files found matching pattern: {pattern}")
        return

    print(f"Found {len(csv_files)} CSV files to process.")

    for csv_file in csv_files:
        # Extract the character ID from the filename (e.g., "character_24436.csv")
        filename = os.path.basename(csv_file)
        if filename.startswith("character_") and filename.endswith(".csv"):
            character_id = filename[len("character_"):-len(".csv")]
            print(f"Processing character ID: {character_id} from file: {csv_file}")

            # Call CharacterCSVParser
            parser = CharacterCSVParser(character_id, csv_file, json_file, add_if_missing=True)
            parser.parse_csv()
            parser.update_json()
        else:
            print(f"Skipping file: {csv_file} (does not match expected pattern)")

def main():
    parser = argparse.ArgumentParser(description="Batch process CSV files and update a JSON file.")
    parser.add_argument("-d", "--directory", required=True, type=str, help="Directory containing CSV files")
    parser.add_argument("-g", "--glob", type=str, default="character_*.csv", help="Glob pattern for CSV files (default: 'character_*.csv')")
    parser.add_argument("-j", "--json", required=True, type=str, help="Path to the characters JSON file")

    args = parser.parse_args()

    # Ensure the directory exists
    if not os.path.exists(args.directory):
        print(f"Error: Directory '{args.directory}' does not exist.")
        return

    # Ensure the JSON file exists
    if not os.path.exists(args.json):
        print(f"Error: JSON file '{args.json}' does not exist.")
        return

    # Process the CSV files
    process_csv_files(args.directory, args.glob, args.json)

if __name__ == "__main__":
    main()