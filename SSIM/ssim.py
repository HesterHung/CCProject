import cv2
from skimage.metrics import structural_similarity as ssim

def calculate_metrics(ground_truth_path, captured_frame_path):
    # 1. Load the images
    img1 = cv2.imread(ground_truth_path)
    img2 = cv2.imread(captured_frame_path)

    # 2. Ensure images are the same size
    if img1.shape != img2.shape:
        img2 = cv2.resize(img2, (img1.shape[1], img1.shape[0]))

    # 3. Convert to grayscale for SSIM
    gray1 = cv2.cvtColor(img1, cv2.COLOR_BGR2GRAY)
    gray2 = cv2.cvtColor(img2, cv2.COLOR_BGR2GRAY)

    # 4. Calculate SSIM
    score, _ = ssim(gray1, gray2, full=True)

    # 5. Calculate PSNR
    psnr_value = cv2.PSNR(img1, img2)

    return score, psnr_value

# --- RUN ANALYSIS ---
# Replace these with your actual filenames
reference = "Ground_Truth_1080p_billinear.png"
test_frame = "Frame_bilinear_StateLinear.png"

ssim_res, psnr_res = calculate_metrics(reference, test_frame)

print(f"Results for {test_frame}:")
print(f"SSIM: {ssim_res:.4f}")
print(f"PSNR: {psnr_res:.2f} dB")
