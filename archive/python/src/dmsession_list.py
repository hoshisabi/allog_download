import json
import argparse
import requests
import time
from bs4 import BeautifulSoup
from auth import AdventurersLeagueAuth

class DmSessionsScraper:
    def __init__(self, json_file="dmsessions.json", delay=0.25):
        self.json_file = json_file
        self.auth = AdventurersLeagueAuth()
        self.auth.login()
        self.session = self.auth.get_session()
        self.user_id = self.auth.get_user_id()
        self.dm_sessions = {}  # Store DM sessions as a map with session ID as the key
        self.delay = delay  # Delay between requests

    def parse_season9_table(self, rows):
        """Parse rows from the season9_format table."""
        for row in rows:
            cells = row.find_all("td")
            if len(cells) < 2:  # Skip rows without enough data
                continue

            # Extract session ID from the link (assuming it's in the href attribute)
            session_link = row.find("a", href=True)
            if not session_link:
                continue
            session_id = session_link["href"].split("/")[-1]

            # Extract character ID and name from the second-to-last cell
            character_cell = cells[-2]
            character_link = character_cell.find("a", href=True)
            character_id = character_link["href"].split("/")[-1] if character_link else None
            character_name = character_link.text.strip() if character_link else None

            # Helper function to standardize values
            def standardize_value(value):
                if value is None:
                    return None
                value = value.strip()
                if value.lower() == "none" or value == "":
                    return None
                return value

            # Parse session data and standardize values
            session_data = {
                "date": standardize_value(cells[0].text),
                "adventure_title": standardize_value(cells[1].text),
                "session_number": standardize_value(cells[2].text),
                "reward": standardize_value(cells[3].text),
                "magic_items": standardize_value(cells[4].text),
                "character": standardize_value(character_name),
                "character_id": standardize_value(character_id),
            }

            # Store the session data in the map using session ID as the key
            self.dm_sessions[session_id] = session_data

    def scrape_session_data(self):
        """Iterates through all pages and extracts DM session details from season9_format."""
        max_page = self.get_max_page()

        for page_num in range(1, max_page + 1):
            dm_sessions_list_url = f"https://www.adventurersleaguelog.com/users/{self.user_id}/dm_log_entries?page={page_num}"
            print(f"Fetching DM session list for page {page_num}...")
            response = self.session.get(dm_sessions_list_url)

            if response.status_code != 200:
                print(f"Error fetching session list for page {page_num}. Skipping.")
                continue

            soup = BeautifulSoup(response.text, "html.parser")

            # Find the season9_format table
            table = soup.find("div", class_=lambda c: c and "season9_format" in c)
            if table:
                rows = table.select("tbody tr")
                self.parse_season9_table(rows)

            # Delay between requests
            print(f"Waiting for {self.delay} seconds before the next request...")
            time.sleep(self.delay)

    def get_max_page(self):
        """Fetches the user's DM Sessions list and determines the highest page number."""
        dm_sessions_list_url = f"https://www.adventurersleaguelog.com/users/{self.user_id}/dm_log_entries?page=1"
        response = self.session.get(dm_sessions_list_url)

        if response.status_code != 200:
            print("Error fetching DM Sessions list page.")
            return 1  # Assume at least page 1

        soup = BeautifulSoup(response.text, "html.parser")
        
        # Locate the `>>` pagination link
        last_page_link = soup.find("a", string=">>")
        
        if last_page_link:
            max_page = int(last_page_link["href"].split("page=")[-1].split("&")[0])
            print(f"Max DM session list page detected: {max_page}")
            return max_page
        else:
            print("Could not find pagination, assuming single page.")
            return 1  # Default to page 1 if pagination isn't detected

    def save_json(self, output_file=None):
        """Saves collected session list data to JSON."""
        file_to_write = output_file or self.json_file
        with open(file_to_write, "w", encoding="utf-8") as file:
            json.dump(self.dm_sessions, file, indent=4)
        print(f"DM Session data successfully written to {file_to_write}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Scrape all DM Session list data and save to JSON.")
    parser.add_argument("-j", "--jsonfile", type=str, help="Specify an alternate output file")
    parser.add_argument("--delay", type=float, default=0.25, help="Delay (in seconds) between requests (default: 0.25)")

    args = parser.parse_args()

    scraper = DmSessionsScraper(json_file=args.jsonfile or "dmsessions.json", delay=args.delay)
    scraper.scrape_session_data()
    scraper.save_json(args.jsonfile)

