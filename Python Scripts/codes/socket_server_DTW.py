import base64
import cv2
import mediapipe as mp
import numpy as np
import asyncio
import websockets
import json
import sys
from fastdtw import fastdtw
from scipy.spatial.distance import euclidean
from datetime import datetime
import traceback

reference_pose_sequence = []
player_pose_sequence = []
frame_numbers = []
distances = []

current_song = ""
current_app_path = ""

buffer_size = 30
offset = 0


pose = mp.solutions.pose.Pose(
    min_detection_confidence=0.8,
    min_tracking_confidence=0.8,
    model_complexity=1,
    enable_segmentation=True,
    smooth_segmentation=True,
    smooth_landmarks=True)

# Function to load reference poses from a JSON file
def load_reference_poses(json_file_path):
    global reference_pose_sequence

    # Open and load the JSON file
    with open(json_file_path, 'r') as file:
        data = json.load(file)

        # Extract the relative angles from each frame and store them in the reference_pose_sequence
        for frame_data in data:
            # Assuming the JSON contains a list of frames
            relative_angles = list(frame_data['relative_angles'].values())  # Convert dict values to a list
            reference_pose_sequence.append(relative_angles)

        print("Reference poses loaded successfully!")

def normalize_angles(angles):
    return [angle / np.pi for angle in angles]

def calculate_relative_angles(landmarks):
    def calculate_relative_angle(a, b, c):
        ba = np.array([a.x - b.x, a.y - b.y])
        bc = np.array([c.x - b.x, c.y - b.y])

        cosine_angle = np.dot(ba, bc) / (np.linalg.norm(ba) * np.linalg.norm(bc))
        angle = np.arccos(np.clip(cosine_angle, -1.0, 1.0))
        return angle

    landmark_triplets = [
        (11, 13, 15), (12, 14, 16),
        (13, 11, 23), (11, 23, 25), (23, 25, 27),
        (14, 12, 24), (12, 24, 26), (24, 26, 28)
    ]

    relative_angles = []

    for a, b, c in landmark_triplets:
        angle = calculate_relative_angle(landmarks[a], landmarks[b], landmarks[c])
        relative_angles.append(angle)

    return relative_angles

def validate_pose_sequence(sequence):
    for i, angles in enumerate(sequence):
        if len(angles) != 10:
            print(f"Frame {i} has an incorrect number of angles: {len(angles)}")

def mae_distance(x, y):
    """
    Calculate Mean Absolute Error between two pose angle sequences
    """
    return np.mean(np.abs(np.array(x) - np.array(y)))

def set_current_song(selected_song, app_path):
    global current_song, current_app_path
    current_song = selected_song
    current_app_path = app_path
    # Call the function and pass the path to your JSON file
    # load_reference_poses(f"test/{current_song}.json") # test
    load_reference_poses(current_app_path + "/PoseFiles/" + f"{current_song}.json") # production  

async def process_frame_task(websocket, path):
    try:
        player_pose_sequence.clear()
        async for message in websocket:
            try:
                data = json.loads(message)
                dataType = data['type']
                if(dataType == "frame_data"):
                    
                    # Decode and process the image with MediaPipe
                    img_data = base64.b64decode(data['image_data'])
                    current_frame_number = data["frame_number"]
                    nparr = np.frombuffer(img_data, np.uint8)
                    frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
                    # print("received frame", current_frame_number)
                    # Process the frame to get pose landmarks
                    flipWebcam = data["flip"]
                    if (flipWebcam):
                        frame = cv2.flip(frame, 1)
                    results = pose.process(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))

                    if results.pose_landmarks:
                        # Calculate relative angles between body landmarks
                        relative_angles = calculate_relative_angles(results.pose_landmarks.landmark)
                        
                        # Buffer the player's pose for DTW
                        player_pose_sequence.append(relative_angles)
                        frame_numbers.append(current_frame_number)
                        
                        mp.solutions.drawing_utils.draw_landmarks(
                            frame, 
                            results.pose_landmarks, 
                            mp.solutions.pose.POSE_CONNECTIONS,
                            landmark_drawing_spec=mp.solutions.drawing_styles.get_default_pose_landmarks_style()
                        )
                        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
                        filename = f"testImg/annotated_frame_{current_frame_number}_{timestamp}.jpg"
                        # cv2.imwrite(filename, frame)

                        if len(player_pose_sequence) == 1:
                            start_player_frame = current_frame_number # Player's first frame

                        # Keep only the last 30 frames in the buffer
                        if len(player_pose_sequence) > buffer_size:
                            player_pose_sequence.pop(0)  # Keep buffer size to 30 frames

                        # Perform DTW comparison after collecting 30 frames
                        if len(player_pose_sequence) == buffer_size:

                            
                            print(f"frames: {frame_numbers}")
                            frame_numbers.clear()
                            end_player_frame = current_frame_number # Player's last frame
                            
                            print(f"SHOULD Start at {start_player_frame} Ends at {end_player_frame}")

                            # Get the sliding window range for the reference sequence
                            start_reference_frame = max(0, start_player_frame - offset)
                            normalized_player_sequence = [normalize_angles(frame) for frame in player_pose_sequence]
                            end_reference_frame = min(len(normalized_reference_sequence), current_frame_number + offset)

                            # Extract the reference frames for the sliding window
                            reference_window = normalized_reference_sequence[start_reference_frame:end_reference_frame]
                            # print(reference_window[0])
                            # print(normalized_player_sequence[0])

                            distance, path = fastdtw(normalized_player_sequence, reference_window, dist=mae_distance)
                            print(f"dtw_distance: {distance}")
                            distances.append(distance)

                            score = max(0, 1 - (distance / len(path)))  # Normalize by path length to get average error
                            print(f"normalized score: {score}")


                            # Send DTW result back to Unity
                            # await websocket.send(json.dumps({"dtw_distance": distance}))

                            # Send DTW score back to Unity
                            await websocket.send(json.dumps({"dtw_score": score}))

                            # Clear the buffer after sending the DTW result
                            player_pose_sequence.clear()

                    else:
                        await websocket.send(json.dumps({"error": "No pose detected"}))
                        player_pose_sequence.append([0] * 8)
                elif(dataType == "song_selection"):
                    print("total dtwdistance calculated: ", len(distances))
                    print("------------------------")
                    app_path = data["app_path"]
                    selected_song = data["song_name"]
                    reference_pose_sequence.clear()
                    distances.clear()
                    set_current_song(selected_song,app_path)
                    print(f"Player selected {selected_song}")
                    frame_numbers.clear()
                    player_pose_sequence.clear()
                    
                    normalized_reference_sequence = [normalize_angles(frame) for frame in reference_pose_sequence]


            except Exception as e:
                await websocket.send(json.dumps({"error": str(e)}))
                traceback.print_exc()
                player_pose_sequence.clear()

    except websockets.exceptions.ConnectionClosed:
        print("WebSocket connection closed")
    finally:
        if not websocket.closed:
            await websocket.close()
            print(f"WebSocket connection properly closed from client {websocket.remote_address}.")

async def main():
    server = await websockets.serve(process_frame_task, "localhost", 8139)
    print("Server started on ws://localhost:8139")


    await server.wait_closed()

if __name__ == "__main__":
    asyncio.run(main())