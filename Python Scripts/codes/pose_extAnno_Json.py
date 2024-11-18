import cv2
import json
import sys
import mediapipe as mp
import numpy as np
from mediapipe.python.solutions.pose import PoseLandmark
import time

def initialize_pose():
    return mp.solutions.pose.Pose(
        min_detection_confidence=0.8,
        min_tracking_confidence=0.8,
        model_complexity=2,
        enable_segmentation=True,
        smooth_segmentation=True,
        smooth_landmarks=True
    )

def initialize_video_writer(video_capture, output_path):
    frame_height = int(video_capture.get(cv2.CAP_PROP_FRAME_HEIGHT))
    frame_width = int(video_capture.get(cv2.CAP_PROP_FRAME_WIDTH))
    fps = video_capture.get(cv2.CAP_PROP_FPS)
    fourcc = cv2.VideoWriter_fourcc(*'mp4v')
    return cv2.VideoWriter(output_path, fourcc, fps, (frame_width, frame_height))


def process_frame(frame, pose, frame_count, timestamp):
    frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    frame_rgb.flags.writeable = False
    results = pose.process(frame_rgb)
    frame_rgb.flags.writeable = True
    frame_bgr = cv2.cvtColor(frame_rgb, cv2.COLOR_RGB2BGR)
    
    return frame_bgr, results

def draw_pose_landmarks(frame, pose_landmarks):
    mp.solutions.drawing_utils.draw_landmarks(
        frame, 
        pose_landmarks, 
        mp.solutions.pose.POSE_CONNECTIONS,
        landmark_drawing_spec=mp.solutions.drawing_styles.get_default_pose_landmarks_style()
    )

def add_text_into_vid_frame(frame, frame_count, timestamp):
    cv2.putText(
        frame,  
        f'frame {frame_count}  {timestamp}',  
        (50, 50),  
        cv2.FONT_HERSHEY_SIMPLEX, 1,  
        (0, 255, 255),  
        2,  
        cv2.LINE_4
    ) 

def extract_landmark_data(pose_landmarks, previous_landmarks):
    if pose_landmarks:
        current_landmarks = []
        for i, landmark in enumerate(pose_landmarks.landmark):
            if landmark.x == 0 and landmark.y == 0 and previous_landmarks is not None:
                current_landmarks.append(previous_landmarks[i])
            else:
                current_landmarks.append(landmark)
        return current_landmarks
    return None

def calculate_metrics(landmarks):
    def calculate_relative_angle(a, b, c):        
        ba = np.array([a.x - b.x, a.y - b.y])
        bc = np.array([c.x - b.x, c.y - b.y])
        
        cosine_angle = np.dot(ba, bc) / (np.linalg.norm(ba) * np.linalg.norm(bc))
        angle = np.arccos(np.clip(cosine_angle, -1.0, 1.0))
        return angle
    
    landmark_triplets = [
        (11, 13, 15), (12, 14, 16),
        (13, 11, 23), (11, 23, 25), (23, 25, 27),
        (14, 12, 24), (12, 24, 26), (24, 26, 28),
        (11, 12, 24), (12, 11, 23)
    ]

    relative_angles = []

    for a, b, c in landmark_triplets:
        angle = calculate_relative_angle(landmarks[a], landmarks[b], landmarks[c])
        relative_angles.append(angle)

    return relative_angles

def initialize_json_file(json_file_path):
    with open(json_file_path, 'w') as json_file:
        json.dump([], json_file)

def append_to_json_file(json_file_path, data):
    with open(json_file_path, 'r+') as json_file:
        file_data = json.load(json_file)
        file_data.append(data)
        json_file.seek(0)
        json.dump(file_data, json_file, indent=2)

def main():
    # change the input path here ("file path" + "video name")
    input_vid_name = "HurryUpPun"
    input_vid_path = "Python Scripts/origin_vids/" + input_vid_name + ".mp4"

    # change the output file paths here
    # c = model complexity
    output_video_path = f"Python Scripts/results/videos/{input_vid_name}_" + "legacy.mp4"
    # r = relative angle
    output_json_path = f"Python Scripts/results/poses/{input_vid_name}_" + "legacy.json"

    pose = initialize_pose()
    vid = cv2.VideoCapture(input_vid_path)
    vid_writer = initialize_video_writer(vid, output_video_path)

    initialize_json_file(output_json_path)

    frame_count = 0
    previous_landmarks = None

    while True:
        ret, frame = vid.read()
        if not ret:
            break
        
        # annotate video
        frame, results = process_frame(frame, pose, frame_count, vid.get(cv2.CAP_PROP_POS_MSEC))
        
        if results.pose_landmarks:
            draw_pose_landmarks(frame, results.pose_landmarks)
        
        # add text to the frame, then write it on output video
        add_text_into_vid_frame(frame, frame_count, vid.get(cv2.CAP_PROP_POS_MSEC))
        vid_writer.write(frame)

        json_data = {
            "frame_number": frame_count,
            "timestamp": vid.get(cv2.CAP_PROP_POS_MSEC)
        }

        current_landmarks = extract_landmark_data(results.pose_landmarks, previous_landmarks)
        
        if current_landmarks:
            # other metrics
            arr_of_rel_angles = calculate_metrics(current_landmarks)
            json_data["relative_angles"] = {f"R{i}": angle for i, angle in enumerate(arr_of_rel_angles)}
            previous_landmarks = current_landmarks
        else:
            if previous_landmarks:
                current_landmarks = previous_landmarks
                arr_of_rel_angles = calculate_metrics(current_landmarks)
                json_data["relative_angles"] = {f"R{i}": angle for i, angle in enumerate(arr_of_rel_angles)}
            else:    
                json_data["relative_angles"] = {f"R{i}": 0.0 for i in range(10)}

        append_to_json_file(output_json_path, json_data)

        frame_count += 1

    pose.close()
    vid.release()
    vid_writer.release()

if __name__ == "__main__":
    start_time = time.time()
    main()
    print(time.time() - start_time)