import json
import os
import argparse
import time
from csv_download import CSVDownloader

def download_csv_for_character(character_id, output_dir, delay):
    """Download the CSV for a specific character ID."""
    output_csv = os.path.join(output_dir, f"character_{character_id}.csv")
    print(f"Downloading CSV for character ID {character_id} to {output_csv}")
    downloader = CSVDownloader(character_id)
    downloader.download_csv(output_csv)
    print(f"Waiting for {delay} seconds before the next request...")
    time.sleep(delay)

def main():
    parser = argparse.ArgumentParser(description="Batch download CSVs for all characters in a JSON file.")
    parser.add_argument("-j", "--json", required=True, type=str, help="Path to the characters JSON file")
    parser.add_argument("-d", "--dir", required=True, type=str, help="Output directory for CSV files")
    parser.add_argument("--delay", type=float, default=0.25, help="Delay (in seconds) between requests (default: 0.25)")

    args = parser.parse_args()

    # Load the characters.json file
    if not os.path.exists(args.json):
        print(f"Error: JSON file '{args.json}' does not exist.")
        return

    with open(args.json, "r", encoding="utf-8") as file:
        try:
            characters = json.load(file)
        except json.JSONDecodeError:
            print(f"Error: Failed to parse JSON file '{args.json}'.")
            return

    # Ensure the output directory exists
    os.makedirs(args.dir, exist_ok=True)

    # Iterate over character IDs and download CSV for each
    for character_id in characters.keys():
        print(f"Processing character ID: {character_id}")
        try:
            download_csv_for_character(character_id, args.dir, args.delay)
        except Exception as e:
            print(f"Error: Failed to download CSV for character ID {character_id}. {e}")

if __name__ == "__main__":
    main()