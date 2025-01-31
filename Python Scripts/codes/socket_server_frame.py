import base64
import cv2
import mediapipe as mp
import numpy as np
import asyncio
import websockets
import json
import sys
import traceback
from datetime import datetime

reference_pose_sequence = []
correctness_sequence = []
window_size = 8

current_song = ""
current_app_path = ""

pose = mp.solutions.pose.Pose(
    static_image_mode=True,
    min_detection_confidence=0.8,
    min_tracking_confidence=0.8,
    model_complexity=1,
    enable_segmentation=True)

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

def set_current_song(selected_song, app_path):
    global current_song, current_app_path
    current_song = selected_song
    current_app_path = app_path
    # Call the function and pass the path to JSON file
    load_reference_poses(current_app_path + "/PoseFiles/" + f"{current_song}.json") # production  

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



async def process_frame(websocket, path):
    global correctness_sequence

    try:
        async for message in websocket:
            try:
                data = json.loads(message)
                dataType = data['type']
                if(dataType == "frame_data"):
                    frame_number = data['frame_number']
                    img_data = data['image_data']
                    print(f"processing frame#{frame_number}")

                    # Decode the base64 image
                    img_data = base64.b64decode(img_data)
                    nparr = np.frombuffer(img_data, np.uint8)
                    frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
                    flipWebcam = data["flip"]
                    print(f"flipWebcam {flipWebcam}")
                    if (flipWebcam):
                        frame = cv2.flip(frame, 1)


                    # Process the frame with MediaPipe
                    results = pose.process(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
                    
                    # Extract pose landmarks
                    if results.pose_landmarks:
                        """
                        # Draw pose landmarks on the frame
                        mp.solutions.drawing_utils.draw_landmarks(
                            frame, 
                            results.pose_landmarks, 
                            mp.solutions.pose.POSE_CONNECTIONS,
                            landmark_drawing_spec=mp.solutions.drawing_styles.get_default_pose_landmarks_style()
                        )
                        
                        # Save the annotated frame
                        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
                        filename = f"gameplay2/annotated_frame_{frame_number}_{timestamp}.jpg"
                        cv2.imwrite(filename, frame)
                        """

                        current_player_pose = calculate_relative_angles(results.pose_landmarks.landmark)
                        print(current_player_pose)
                        json_data = {}
                        # json_data["relative_angles"] = {f"R{i}": angle for i, angle in enumerate(arr_of_rel_angles)}


                        
                        start_reference_frame = max([frame_number - 12, 0])
                        end_reference_frame = min([frame_number + 4, len(reference_pose_sequence)-1])
                        reference_window = reference_pose_sequence[start_reference_frame:end_reference_frame]
                        print(f"start{start_reference_frame} end{end_reference_frame}")
                        print(f"attempt to compare ref {frame_number}")

                        frame_correctness_list = []
                        for reference_pose in reference_window:
                            differences = []

                            for i in range(len(reference_pose)):
                                normalized_difference = min(abs(reference_pose[i] - current_player_pose[i]) , 1)
                                differences.append(normalized_difference)

                            correctness = max(0, 1 - (sum(differences) / len(differences)))
                            frame_correctness_list.append(correctness)
                        current_frame_correctness = max(frame_correctness_list)
                        print(f"Current Frame correctness: {current_frame_correctness}")
                        print(f"Best matching frame is: {start_reference_frame + frame_correctness_list.index(current_frame_correctness)}")
                        correctness_sequence.append(current_frame_correctness)
                        
                        if(len(correctness_sequence)==3):
                            print(f"Max of correctness sequence (3): {max(correctness_sequence)}")
                            correctness_sequence.clear()
                        
                        json_data["correctness"] = current_frame_correctness
                        await websocket.send(json.dumps(json_data))
                        # print(f"Successfully sent frame#{frame_number}'s data")
                    else:
                        print("No pose detected")
                        await websocket.send(json.dumps({"correctness":0.0}))
                elif(dataType == "song_selection"):
                    print("------------------------")
                    app_path = data["app_path"]
                    selected_song = data["song_name"]
                    reference_pose_sequence.clear()
                    set_current_song(selected_song,app_path)
                    correctness_sequence.clear()
                    print(f"Player selected {selected_song}")
            except asyncio.exceptions.IncompleteReadError:
                print("Incomplete read error occurred. The client might have disconnected.")
                break
            except Exception as e:
                print(f"An error occurred while processing the frame: {str(e)}")
                traceback.print_exc()
                await websocket.send(json.dumps({"error": str(e)}))
    except websockets.exceptions.ConnectionClosed:
        print("WebSocket connection closed")

async def main():
    server = await websockets.serve(process_frame, "localhost", 8139)
    print("Server started on ws://localhost:8139")
    await server.wait_closed()

if __name__ == "__main__":
    asyncio.run(main())