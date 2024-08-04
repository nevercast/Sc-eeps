import os
import zipfile
import http.client
import json

# Load environment variables for credentials
SCREEPS_TOKEN = os.getenv('SCREEPS_TOKEN')

# Fixed variables
SCREEPS_HOST = 'screeps.com'
ZIP_FILE_PATH = 'bot.zip'

def upload_bot(zip_file_path, token):
    # Extract files from the zip
    modules = {}
    with zipfile.ZipFile(zip_file_path, 'r') as zip_ref:
        for file_info in zip_ref.infolist():
            with zip_ref.open(file_info) as file:
                file_content = file.read().decode('utf-8')
                module_name = file_info.filename.replace('.js', '').replace('.wasm', '')
                modules[module_name] = file_content

    # Prepare data for setting the code
    data = {
        'branch': 'default',
        'modules': modules
    }

    # Set code on Screeps
    conn = http.client.HTTPSConnection(SCREEPS_HOST)
    headers = {
        'Content-Type': 'application/json; charset=utf-8',
        'X-Token': token,
    }
    conn.request('POST', '/api/user/code', body=json.dumps(data), headers=headers)
    response = conn.getresponse()
    response_data = response.read().decode('utf-8')
    conn.close()

    if response.status != 200:
        raise Exception(f"Failed to upload bot code: {response.status} {response.reason}\n{response_data}")

    return json.loads(response_data)

if __name__ == '__main__':
    if not SCREEPS_TOKEN:
        print("Screeps token is not set. Please set SCREEPS_TOKEN environment variable.")
        exit(1)

    upload_response = upload_bot(ZIP_FILE_PATH, SCREEPS_TOKEN)
    print("Bot code uploaded successfully:", upload_response)
