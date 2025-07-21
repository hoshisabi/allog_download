import json
import csv
import zipfile
import os
import argparse

def flatten_json(y):
    out = {}

    def flatten(x, name=''):
        if type(x) is dict:
            for a in x:
                flatten(x[a], name + a + '_')
        elif type(x) is list:
            i = 0
            for a in x:
                flatten(a, name + str(i) + '_')
                i += 1
        else:
            out[name[:-1]] = x

    flatten(y)
    return out

def json_to_csv_zip(json_file_path, output_zip_path):
    with open(json_file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    characters_data = []
    sessions_data = []
    magic_items_data = []

    for char_id, char_info in data.items():
        # Extract main character data
        character_row = {
            'id': char_id,
            'name': char_info.get('name', ''),
            'race': char_info.get('race', ''),
            'class': char_info.get('class', ''),
            'level': char_info.get('level', ''),
            'season': char_info.get('season', ''),
            'tag': char_info.get('tag', '')
        }
        characters_data.append(character_row)

        # Extract sessions data
        for session in char_info.get('sessions', []):
            session_row = {'character_id': char_id}
            session_row.update(session)
            sessions_data.append(session_row)

        # Extract magic items data
        for item in char_info.get('magic_items', []):
            item_row = {'character_id': char_id}
            item_row.update(item)
            magic_items_data.append(item_row)

    # Define CSV file paths
    temp_dir = 'temp_csv_output'
    os.makedirs(temp_dir, exist_ok=True)

    characters_csv_path = os.path.join(temp_dir, 'characters.csv')
    sessions_csv_path = os.path.join(temp_dir, 'sessions.csv')
    magic_items_csv_path = os.path.join(temp_dir, 'magic_items.csv')

    # Write characters.csv
    if characters_data:
        characters_fieldnames = list(characters_data[0].keys())
        with open(characters_csv_path, 'w', newline='', encoding='utf-8') as csvfile:
            writer = csv.DictWriter(csvfile, fieldnames=characters_fieldnames)
            writer.writeheader()
            writer.writerows(characters_data)

    # Write sessions.csv
    if sessions_data:
        # Collect all possible fieldnames from all session dictionaries
        sessions_fieldnames = sorted(list(set(key for d in sessions_data for key in d)))
        with open(sessions_csv_path, 'w', newline='', encoding='utf-8') as csvfile:
            writer = csv.DictWriter(csvfile, fieldnames=sessions_fieldnames)
            writer.writeheader()
            writer.writerows(sessions_data)

    # Write magic_items.csv
    if magic_items_data:
        # Collect all possible fieldnames from all magic_item dictionaries
        magic_items_fieldnames = sorted(list(set(key for d in magic_items_data for key in d)))
        with open(magic_items_csv_path, 'w', newline='', encoding='utf-8') as csvfile:
            writer = csv.DictWriter(csvfile, fieldnames=magic_items_fieldnames)
            writer.writeheader()
            writer.writerows(magic_items_data)

    # Create zip archive
    with zipfile.ZipFile(output_zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        if os.path.exists(characters_csv_path):
            zipf.write(characters_csv_path, os.path.basename(characters_csv_path))
        if os.path.exists(sessions_csv_path):
            zipf.write(sessions_csv_path, os.path.basename(sessions_csv_path))
        if os.path.exists(magic_items_csv_path):
            zipf.write(magic_items_csv_path, os.path.basename(magic_items_csv_path))

    # Clean up temporary CSV files
    if os.path.exists(characters_csv_path):
        os.remove(characters_csv_path)
    if os.path.exists(sessions_csv_path):
        os.remove(sessions_csv_path)
    if os.path.exists(magic_items_csv_path):
        os.remove(magic_items_csv_path)
    if os.path.exists(temp_dir):
        os.rmdir(temp_dir)

    print(f"Successfully created {output_zip_path} containing CSV files.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Convert characters JSON to multiple CSV files and zip them.")
    parser.add_argument("-j", "--json", required=True, help="Path to the input characters JSON file")
    parser.add_argument("-o", "--output", required=True, help="Path to the output ZIP file")
    args = parser.parse_args()

    json_to_csv_zip(args.json, args.output)
