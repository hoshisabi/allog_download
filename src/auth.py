import requests
from bs4 import BeautifulSoup
import os
import urllib3


class AdventurersLeagueAuth:
    def __init__(self):
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
        self.session = requests.Session()
        self.email = os.getenv("ALLOG_EMAIL")
        self.password = os.getenv("ALLOG_PASSWORD")
        self.login_url = "https://www.adventurersleaguelog.com/users/sign_in"
        self.user_id = None

    def login(self):
        if not self.email or not self.password:
            raise ValueError("ALLOG_EMAIL and ALLOG_PASSWORD must be set!")

        login_page = self.session.get(self.login_url, verify=False)
        soup = BeautifulSoup(login_page.text, "html.parser")
        auth_token = soup.find('input', {'name': 'authenticity_token'})['value']

        payload = {
            "user[email]": self.email,
            "user[password]": self.password,
            "authenticity_token": auth_token
        }

        response = self.session.post(self.login_url, data=payload)

        if "Signed in successfully" in response.text:
            print("Login successful!")
            self.user_id = self.extract_user_id(response.text)
        else:
            raise ValueError("Login failed!")

    def extract_user_id(self, page_content):
        soup = BeautifulSoup(page_content, "html.parser")
        profile_links = soup.select("a[href*='/users/']")
        for link in profile_links:
            href = link["href"]
            if "/users/" in href:
                return href.split("/users/")[1].split("/")[0]
        raise ValueError("User ID not found!")

    def get_session(self):
        return self.session

    def get_user_id(self):
        return self.user_id

