import json
import argparse
import os
import zipfile
import re

def generate_markdown(json_file_path, output_path, output_type='single', zip_output=False):
    with open(json_file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    temp_md_dir = None
    if zip_output or output_type == 'per-character':
        temp_md_dir = os.path.join(os.path.dirname(output_path), "temp_markdown_output")
        os.makedirs(temp_md_dir, exist_ok=True)

    markdown_files_to_zip = []

    if output_type == 'single':
        markdown_content = "# Adventurers League Character Logbook\n\n"
        for char_id, char_info in data.items():
            markdown_content += f"## {char_info.get('name', 'Unknown Character')} (ID: {char_id})\n\n"
            markdown_content += "### Character Details\n\n"
            markdown_content += f"* **Race:** {char_info.get('race', '')}\n"
            markdown_content += f"* **Class:** {char_info.get('class', '')}\n"
            markdown_content += f"* **Level:** {char_info.get('level', '')}\n"
            
            total_gp = sum(s.get('gp_gained', 0) for s in char_info.get('sessions', []) if s.get('gp_gained') is not None)
            total_downtime = sum(s.get('downtime_gained', 0) for s in char_info.get('sessions', []) if s.get('downtime_gained') is not None)

            markdown_content += f"* **Total GP:** {total_gp:.2f}\n"
            markdown_content += f"* **Total Downtime:** {total_downtime:.2f}\n"

            if char_info.get('season'):
                markdown_content += f"* **Season:** {char_info.get('season', '')}\n"
            if char_info.get('tag'):
                markdown_content += f"* **Tag:** {char_info.get('tag', '')}\n"
            if char_info.get('faction'):
                markdown_content += f"* **Faction:** {char_info.get('faction', '')}\n"
            markdown_content += "\n"

            # Magic Items (moved to top, as bulleted list)
            magic_items = char_info.get('magic_items', [])
            if magic_items:
                markdown_content += "### Magic Items\n\n"
                for item in magic_items:
                    item_str = f"* {item.get('name', '')}"
                    details = []
                    if item.get('rarity'):
                        details.append(f"Rarity: {item.get('rarity')}")
                    if item.get('location_found'):
                        details.append(f"Found: {item.get('location_found')}")
                    if details:
                        item_str += f" ({', '.join(details)})"
                    markdown_content += f"{item_str}\n"
                markdown_content += "\n"

            sessions = char_info.get('sessions', [])
            if sessions:
                markdown_content += "### Adventure Log\n\n"
                markdown_content += "| Adventure Title | Date Played | Session Length (hrs) | Player Level | GP Gained | Downtime Gained | DM Name | Location | Notes |\n"
                markdown_content += "|---|---|---|---|---|---|---|---|---|\n"
                for session in sessions:
                    markdown_content += f"| {session.get('adventure_title', '')}"
                    markdown_content += f"| {session.get('date_played', '').split(' ')[0] if session.get('date_played') else ''}"
                    markdown_content += f"| {session.get('session_length_hours', '')}"
                    markdown_content += f"| {session.get('player_level', '')}"
                    markdown_content += f"| {session.get('gp_gained', '')}"
                    markdown_content += f"| {session.get('downtime_gained', '')}"
                    markdown_content += f"| {session.get('dm_name', '')}"
                    markdown_content += f"| {session.get('location_played', '')}"
                    markdown_content += f"| {session.get('notes', '').replace('\n', ' ')}"
                    markdown_content += "|\n"
                markdown_content += "\n"

        if zip_output:
            single_md_path = os.path.join(temp_md_dir, os.path.basename(output_path).replace('.zip', '.md'))
            with open(single_md_path, 'w', encoding='utf-8') as f:
                f.write(markdown_content)
            markdown_files_to_zip.append(single_md_path)
        else:
            with open(output_path, 'w', encoding='utf-8') as f:
                f.write(markdown_content)
            print(f"Successfully generated Markdown file: {output_path}")

    elif output_type == 'per-character':
        for char_id, char_info in data.items():
            char_name_raw = char_info.get('name', 'Unknown_Character')
            # Sanitize character name for filename
            char_name_sanitized = re.sub(r'[<>:"/\\|?*]', '_', char_name_raw) # Replace invalid characters with underscore
            char_name_sanitized = char_name_sanitized.replace(' ', '_') # Replace spaces with underscore

            filename = f"{char_name_sanitized}_{char_id}.md"
            char_md_path = os.path.join(temp_md_dir, filename)

            char_markdown_content = f"# {char_info.get('name', 'Unknown Character')} (ID: {char_id})\n\n"
            char_markdown_content += "### Character Details\n\n"
            char_markdown_content += f"* **Race:** {char_info.get('race', '')}\n"
            char_markdown_content += f"* **Class:** {char_info.get('class', '')}\n"
            char_markdown_content += f"* **Level:** {char_info.get('level', '')}\n"

            total_gp = sum(s.get('gp_gained', 0) for s in char_info.get('sessions', []) if s.get('gp_gained') is not None)
            total_downtime = sum(s.get('downtime_gained', 0) for s in char_info.get('sessions', []) if s.get('downtime_gained') is not None)

            char_markdown_content += f"* **Total GP:** {total_gp:.2f}\n"
            char_markdown_content += f"* **Total Downtime:** {total_downtime:.2f}\n"

            if char_info.get('season'):
                char_markdown_content += f"* **Season:** {char_info.get('season', '')}\n"
            if char_info.get('tag'):
                char_markdown_content += f"* **Tag:** {char_info.get('tag', '')}\n"
            if char_info.get('faction'):
                char_markdown_content += f"* **Faction:** {char_info.get('faction', '')}\n"
            char_markdown_content += "\n"

            # Magic Items (moved to top, as bulleted list)
            magic_items = char_info.get('magic_items', [])
            if magic_items:
                char_markdown_content += "### Magic Items\n\n"
                for item in magic_items:
                    item_str = f"* {item.get('name', '')}"
                    details = []
                    if item.get('rarity'):
                        details.append(f"Rarity: {item.get('rarity')}")
                    if item.get('location_found'):
                        details.append(f"Found: {item.get('location_found')}")
                    if details:
                        item_str += f" ({', '.join(details)})"
                    char_markdown_content += f"{item_str}\n"
                char_markdown_content += "\n"

            sessions = char_info.get('sessions', [])
            if sessions:
                char_markdown_content += "### Adventure Log\n\n"
                char_markdown_content += "| Adventure Title | Date Played | Session Length (hrs) | Player Level | GP Gained | Downtime Gained | DM Name | Location | Notes |\n"
                char_markdown_content += "|---|---|---|---|---|---|---|---|---|\n"
                for session in sessions:
                    char_markdown_content += f"| {session.get('adventure_title', '')}"
                    char_markdown_content += f"| {session.get('date_played', '').split(' ')[0] if session.get('date_played') else ''}"
                    char_markdown_content += f"| {session.get('session_length_hours', '')}"
                    char_markdown_content += f"| {session.get('player_level', '')}"
                    char_markdown_content += f"| {session.get('gp_gained', '')}"
                    char_markdown_content += f"| {session.get('downtime_gained', '')}"
                    char_markdown_content += f"| {session.get('dm_name', '')}"
                    char_markdown_content += f"| {session.get('location_played', '')}"
                    char_markdown_content += f"| {session.get('notes', '').replace('\n', ' ')}"
                    char_markdown_content += "|\n"
                char_markdown_content += "\n"

            with open(char_md_path, 'w', encoding='utf-8') as f:
                f.write(char_markdown_content)
            markdown_files_to_zip.append(char_md_path)
            print(f"Generated Markdown file for {char_name_raw}: {char_md_path}")

    if zip_output:
        with zipfile.ZipFile(output_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
            for md_file in markdown_files_to_zip:
                zipf.write(md_file, os.path.basename(md_file))
        print(f"Successfully created ZIP archive: {output_path}")

    # Clean up temporary Markdown files and directory
    if temp_md_dir and os.path.exists(temp_md_dir):
        for f in os.listdir(temp_md_dir):
            os.remove(os.path.join(temp_md_dir, f))
        os.rmdir(temp_md_dir)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Convert characters JSON to Markdown.")
    parser.add_argument("-j", "--json", required=True, help="Path to the input characters JSON file")
    parser.add_argument("-o", "--output", required=True, help="Path to the output Markdown file or ZIP archive")
    parser.add_argument("-t", "--output-type", choices=['single', 'per-character'], default='single',
                        help="Type of Markdown output: 'single' file or 'per-character' files.")
    parser.add_argument("-z", "--zip", action='store_true', help="Zip the output Markdown file(s).")
    args = parser.parse_args()

    generate_markdown(args.json, args.output, args.output_type, args.zip)
