import argparse
import requests
from auth import AdventurersLeagueAuth

class CSVDownloader:
    def __init__(self, character_id):
        self.auth = AdventurersLeagueAuth()
        self.auth.login()
        self.session = self.auth.get_session()
        self.user_id = self.auth.get_user_id()
        self.character_id = character_id
        self.csv_url = f"https://www.adventurersleaguelog.com/users/{self.user_id}/characters/{character_id}.csv"

    def download_csv(self, filename=None):
        response = self.session.get(self.csv_url)
        if response.status_code == 200:
            filename = filename or f"character_{self.character_id}.csv"
            with open(filename, "wb") as file:
                file.write(response.content)
            print(f"CSV downloaded successfully as {filename}!")
        else:
            print(f"Failed to fetch CSV, status: {response.status_code}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Download a CSV for a given character ID.")
    parser.add_argument("-i", "--character_id", required=True, type=str, help="Number of the character to download")
    parser.add_argument("-c", "--csvfile", required=True, type=str, help="Optional output filename")

    args = parser.parse_args()

    print(f"downlaoding character id {args.character_id} to {args.csvfile}")


    downloader = CSVDownloader(args.character_id)
    downloader.download_csv(args.csvfile)

