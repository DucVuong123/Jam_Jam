# Prompt tạo art Wool Jam 2D

Công cụ: built-in imagegen, transparent_background=true. Hai ảnh người dùng gửi được dùng làm tham chiếu nhận dạng/phong cách; không dùng làm background. File gốc của imagegen được giữ tại thư mục generated_images, bản sử dụng trong game được copy vào project.

## Mèo

Output cuối: Assets/JamJam/Art/Sprites/Kitten-Wool.png

Prompt tạo ban đầu:

Create a high-resolution game-ready transparent PNG sprite faithfully matching the supplied kitten reference. The input image is the sole CHARACTER IDENTITY / STYLE reference, not a background to reproduce. Reconstruct a complete adorable orange knitted yarn kitten, sitting upright, front facing with very slight top-down mobile-game perspective, large round glossy black eyes with white catchlights, triangular pointed orange ears with pink inner ears, warm golden yellow muzzle and chest, small pink nose, a sweet friendly smile, short rounded paws, orange striped knitted forehead and cheeks, and a short curled tail resting along its left side as in the reference. Closely preserve the reference kitten's face shape, round head/body proportions and rich braided stitch texture, rather than inventing a different cat. Soft polished 3D-rendered knitted toy aesthetic baked into a 2D sprite, smooth clean silhouette and dense fine yarn texture. Full character visible including tips of ears, tail and paws, centered with generous transparent padding on all sides. Bright orange/golden colors from the reference, soft neutral lighting with gentle ambient shading within the character. Straight upright portrait orientation. No floor, no vegetation, no yarn ball, no background, no frame, no text, no watermark, no external cast shadow. Genuine alpha transparency. This will replace a Unity kitten model and must read clearly at around 80 screen pixels tall.

Prompt chỉnh bản cuối:

Edit this transparent game sprite with ONLY these targeted changes: completely remove the red neck collar and golden bell, replacing their area with continuous natural orange and cream knitted neck/chest. Shorten the raised tail into a short rounded tail curling along the kitten's lower left side, below shoulder height, like a seated baby kitten. Preserve the exact face, large glossy eyes, expression, pointed ears, head/body proportions, knitted orange tabby stitch texture, paws, lighting and centered upright composition. Do not add any accessory, clothing, jewelry, yarn ball, text, background or cast shadow. Keep the complete kitten visible with transparent padding and genuine alpha transparency.

## Đầu rồng

Output: Assets/JamJam/Art/Sprites/Dragon-Head-Wool.png

Create one high-resolution game-ready transparent PNG sprite closely matching the exact knitted dragon head in the supplied tiny reference. Reference role: CHARACTER IDENTITY, palette, pose and facial expression. Faithfully reconstruct the complete head and only a very short neck attachment, not a new dragon design. Orange rounded broad knitted muzzle, orange face with fine woven stitch texture, sleepy narrowed expressive white eyes with dark blue pupils and orange brow ridges, small nostrils, cream pointed tooth fangs at the mouth corners, a distinctive white teardrop/diamond-shaped small central forehead horn, orange-golden ear fins and layered purple-and-gold knitted mane/scales around the rear top of the head. Match the reference's slightly mischievous serious expression and stylized rendered toy look. Three-quarter view from slightly above with forehead/rear neck toward upper left and snout pointing downward and slightly right, approximately 60 degrees clockwise below the screen's horizontal right direction, exactly the reference angle. Include the whole head, ears, horn, chin and the purple/gold mane silhouette; do not crop them. Polished soft 3D-rendered yarn texture baked into a 2D game sprite with clean alpha edges, bright saturated orange/purple/gold, neutral soft lighting and internal shading. Generous transparent padding. No full body, no limbs, no flame, no background, no ground, no track, no cat, no text, no watermark, no external cast shadow. Genuine transparent alpha background. Unity sprite will be around 100 screen pixels tall; readable silhouette and eyes are essential.

## Block

Block2D.png và Arrow2D.png được tạo bằng code Unity trong WoolAssetsBuilder; dùng cùng hoa văn Stitch của project, không dùng imagegen. Các hình mũi tên và nền khối là hình đơn giản phù hợp dựng trực tiếp bằng code.

## API import đã xác minh

[TextureImporterSettings.spriteMeshType](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/textureimportersettings/spritemeshtype), [TextureImporter.SetTextureSettings](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/textureimporter/settexturesettings).
