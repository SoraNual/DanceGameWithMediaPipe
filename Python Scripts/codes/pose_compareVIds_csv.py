import cv2
import csv
import sys
import mediapipe as mp
import numpy as np
from mediapipe.python.solutions.pose import PoseLandmark
from statistics import mode

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

def get_feedback_scores(correctness_percentage):
    if correctness_percentage >= 80: return "perfect"
    elif correctness_percentage >= 60: return "cool"
    elif correctness_percentage >= 40: return "passable"
    return "not good enough"

def get_correctness_percentage(correctness):
    return correctness * 100

def add_frame_info(frame, frame_count, current_values, ref_values, correctness, frame_feedback, total_feedbacks, mode_feedback, average_feedback, timestamp):
    lines = [f'frame {frame_count}  {timestamp}',
             f'cur {current_values}',
             f'ref {ref_values}',
             f'correctness {correctness}',
             f'{frame_feedback}',
             f'mode {mode_feedback}',
             f'mean {average_feedback}']
    
    for i in total_feedbacks.keys():
        for j in total_feedbacks[i].keys():
            lines.append(f'{j}: {total_feedbacks['frame'][j]} {total_feedbacks['average'][j]} {total_feedbacks['mode'][j]}')
        break
    
    gap = 50
    for i in range(len(lines)):
        cv2.putText(
            frame,  
            lines[i],  
            (50, 50+i*gap),  
            cv2.FONT_HERSHEY_SIMPLEX, 0.8,  
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
    threshold = 0.5    
    def calculate_relative_angle(a, b, c):
        """
        Calculate the angle between vectors ba and bc
        a: central point (e.g., elbow)
        b: start point (e.g., shoulder)
        c: end point (e.g., wrist)
        """
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

def compare_values(current_values, ref_values):
    if sum(ref_values) == 0:
        return None  # if there are no reference values at all.
    comparison_result = []
    for i in range(len(current_values)):
        difference = abs(current_values[i] - ref_values[i])
        comparison_result.append(difference)
    if len(comparison_result) == 0:
        print(ref_values)
        return None
    correctness = 1 - (sum(comparison_result) / len(comparison_result))
    return correctness

def read_ref_poses_csv(relative_angle_csv_path):
    ref_relative_angles_data = []

    with open(relative_angle_csv_path, 'r') as csvfile:
        csv_reader = csv.reader(csvfile)
        header = next(csv_reader)
        for row in csv_reader:
            ref_relative_angles_data.append([float(value) for value in row[2:]])  # Assuming first two columns are frame_number and timestamp

    return ref_relative_angles_data

def get_mode_feedback(feedback_list):
    if len(feedback_list) == 0:
        return "none"
    try:
        return mode(feedback_list)
    except:
        # Handle case where there are multiple modes by picking the first one
        return feedback_list[0]
def get_average_feedback(correctnessList):
    if len(correctnessList) == 0:
        return 0.0
    return sum(correctnessList) / len(correctnessList)
    

def main():
    # Add a parameter to choose between gradients and absolute angles
    comparison_type = 'RelativeAngles'  # 'Gradients' or 'AbsoluteAngles' or 'RelativeAngles' 
    
    # change the input paths here
    player_codename = "wholeGarden_tabletCUT"
    ref_codename = "wholeGarden_webcamCUT" 

    input_video_path = "Python Scripts/origin_vids/" + player_codename + ".mp4"
    
    ref_rel_angles_csv_path = "Python Scripts/results/poses/" + ref_codename +"_legacy_edit.csv"

    
    outdir, inputflnm = input_video_path[:input_video_path.rfind('/')+1], input_video_path[input_video_path.rfind('/')+1:]
    inflnm, inflext = inputflnm.split('.')

    # change the output path here
    output_video_path = "Python Scripts/results/videos" + f"/{inflnm}_annotated_C2"+ f"_with{comparison_type}Scoring_edit" + ".mp4"

    pose = initialize_pose()
    ref_relative_angles_data = read_ref_poses_csv(ref_rel_angles_csv_path)
    vid = cv2.VideoCapture(input_video_path)
    vid_writer = initialize_video_writer(vid, output_video_path)

    feedbacks = {
        'perfect': 0,
        'cool': 0,
        'passable': 0,
        'not good enough': 0
    }

    total_feedbacks = {
    'frame': feedbacks.copy(),
    'average': feedbacks.copy(),
    'mode': feedbacks.copy()
    }

    feedbackList = []
    correctnessList = []
    mode_feedback = 'unclear'
    average_feedback = 'unclear'

    frame_count = 0
    previous_landmarks = None

    while True:
        ret, frame = vid.read()
        if not ret:
            break

        frame, results = process_frame(frame, pose, frame_count, vid.get(cv2.CAP_PROP_POS_MSEC))
        
        if results.pose_landmarks:
            draw_pose_landmarks(frame, results.pose_landmarks)


        current_landmarks = extract_landmark_data(results.pose_landmarks, previous_landmarks)
        
        if current_landmarks:
            relative_angles_array = calculate_metrics(current_landmarks)

            previous_landmarks = current_landmarks

            if frame_count < len(ref_relative_angles_data):
                if comparison_type == 'RelativeAngles':
                    current_values = relative_angles_array
                    ref_values = ref_relative_angles_data[frame_count]
                
                if(ref_values is not None):
                    correctness = compare_values(current_values, ref_values)
                    if correctness is not None:
                        correctness_percentage = get_correctness_percentage(correctness)
                        correctnessList.append(correctness_percentage)
                        # calculate average feedback
                        if(frame_count % 30 == 0):
                            average_feedback_percentage = get_average_feedback(correctnessList)
                            average_feedback = get_feedback_scores(average_feedback_percentage)
                            correctnessList.clear()
                            total_feedbacks['average'][average_feedback] += 1
                        frame_feedback = get_feedback_scores(correctness_percentage)
                        total_feedbacks['frame'][frame_feedback] += 1
                    else:
                        frame_feedback = "none"
            else:
                print(f"No comparison data available for frame #{frame_count}")
                frame_feedback = "none"
                correctness = None
                if comparison_type == 'RelativeAngles':
                    ref_values = [0] * 8
                else:
                    ref_values = [0] * 26

        else:
            frame_feedback = "none"
            correctness = None
            if comparison_type == 'RelativeAngles':
                ref_values = [0] * 8
                current_values = [0] * 8
            else:
                ref_values = [0] * 26
                current_values = [0] * 26

        # calculate mode feedback            
        if(frame_feedback != "none"):
            feedbackList.append(frame_feedback)
        if((frame_count)%30==0):
            mode_feedback = get_mode_feedback(feedbackList)
            feedbackList.clear()
            total_feedbacks['mode'][mode_feedback] += 1
        add_frame_info(frame, frame_count, current_values, ref_values, correctness, frame_feedback, total_feedbacks, mode_feedback, average_feedback, vid.get(cv2.CAP_PROP_POS_MSEC))

        vid_writer.write(frame)

        frame_count += 1


    pose.close()
    vid.release()
    vid_writer.release()

if __name__ == "__main__":
    main()