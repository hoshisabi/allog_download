import json
import argparse
import requests
from bs4 import BeautifulSoup
from auth import AdventurersLeagueAuth

class CharacterScraper:
    def __init__(self, json_file="characters.json"):
        self.json_file = json_file
        self.auth = AdventurersLeagueAuth()
        self.auth.login()
        self.session = self.auth.get_session()
        self.user_id = self.auth.get_user_id()
        self.characters = {}  # Store characters with IDs as keys

    def get_max_page(self):
        """Fetches the user's character list and determines the highest page number."""
        character_list_url = f"https://www.adventurersleaguelog.com/users/{self.user_id}/characters?page=1"
        response = self.session.get(character_list_url, verify=False)

        if response.status_code != 200:
            print("Error fetching character list page.")
            return 1  # Assume at least page 1

        soup = BeautifulSoup(response.text, "html.parser")
        
        # Locate the `>>` pagination link
        last_page_link = soup.find("a", string=">>")
        
        if last_page_link:
            max_page = int(last_page_link["href"].split("page=")[-1].split("&")[0])
            print(f"Max character page detected: {max_page}")
            return max_page
        else:
            print("Could not find pagination, assuming single page.")
            return 1  # Default to page 1 if pagination isn't detected

    def scrape_character_data(self):
        """Iterates through all pages and extracts character details."""
        max_page = self.get_max_page()

        for page_num in range(1, max_page + 1):
            character_list_url = f"https://www.adventurersleaguelog.com/users/{self.user_id}/characters?page={page_num}"
            response = self.session.get(character_list_url, verify=False)

            if response.status_code != 200:
                print(f"Error fetching character list for page {page_num}. Skipping.")
                continue

            soup = BeautifulSoup(response.text, "html.parser")

            # Adjust selectors based on actual webpage structure
            character_rows = soup.select("table tbody tr")  # Look for rows inside a table

            for row in character_rows:
                character_link = row.find("a", href=True)
                if character_link:
                    character_season = row.select_one("td:nth-of-type(1)").text.strip() if row.select_one("td:nth-of-type(1)") else ""
                    character_id_tag = row.select_one("td:nth-of-type(2) a")
                    character_id = character_id_tag["href"].split("/")[-1] if character_id_tag else ""
                    character_name = character_id_tag.text.strip() if character_id_tag else "UNKNOWN"  # Name must exist
                    character_race = row.select_one("td:nth-of-type(3)").text.strip() if row.select_one("td:nth-of-type(3)") else ""
                    character_class = row.select_one("td:nth-of-type(4)").text.strip() if row.select_one("td:nth-of-type(4)") else ""
                    character_level = row.select_one("td:nth-of-type(5)").text.strip() if row.select_one("td:nth-of-type(5)") else ""
                    character_tag = row.select_one("td:nth-of-type(6)").text.strip() if row.select_one("td:nth-of-type(6)") else ""

                    self.characters[character_id] = {
                        "name": character_name,
                        "race": character_race,
                        "class": character_class,
                        "level": character_level,
                        "season": character_season,
                        "tag": character_tag
                    }

    def save_json(self, output_file=None):
        """Saves collected character data to JSON."""
        file_to_write = output_file or self.json_file
        with open(file_to_write, "w", encoding="utf-8") as file:
            json.dump(self.characters, file, indent=4)
        print(f"Character data successfully written to {file_to_write}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Scrape all characters and save to JSON.")
    parser.add_argument("-o", "--output", type=str, help="Specify an alternate output file")

    args = parser.parse_args()

    scraper = CharacterScraper()
    scraper.scrape_character_data()
    scraper.save_json(args.output)

