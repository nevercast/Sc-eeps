import os
import zipfile
import http.client
import json
import sys

# Load environment variables for credentials
SCREEPS_TOKEN = os.getenv('SCREEPS_TOKEN')

# Fixed variables
SCREEPS_HOST = 'screeps.com'

def upload_bot(path, token):
    modules = {}

    if os.path.isfile(path) and path.endswith('.zip'):
        # Extract files from the zip
        with zipfile.ZipFile(path, 'r') as zip_ref:
            for file_info in zip_ref.infolist():
                with zip_ref.open(file_info) as file:
                    file_content = file.read().decode('utf-8')
                    module_name = file_info.filename.replace('.js', '').replace('.wasm', '')
                    modules[module_name] = file_content
    elif os.path.isdir(path):
        # Read files from the directory
        for root, _, files in os.walk(path):
            for file_name in files:
                if file_name.endswith('.js') or file_name.endswith('.wasm'):
                    file_path = os.path.join(root, file_name)
                    with open(file_path, 'r', encoding='utf-8') as file:
                        file_content = file.read()
                        module_name = os.path.relpath(file_path, path).replace('.js', '').replace('.wasm', '')
                        modules[module_name] = file_content
    else:
        raise ValueError("The provided path is neither a .zip file nor a directory")

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

    if len(sys.argv) != 2:
        print("Usage: python upload.py <path_to_zip_or_directory>")
        exit(1)

    path = sys.argv[1]
    upload_response = upload_bot(path, SCREEPS_TOKEN)
    print("Bot code uploaded successfully:", upload_response)
