@echo off
echo ---------- BAT DAU PHASE 5: DONG BO TOAN DIEN ----------
echo Cap nhat dev moi nhat...
git checkout dev
git pull origin dev

echo Tao nhanh chốt hạ: feature/final-integration...
git checkout -b feature/final-integration
git config user.name "Tin Nguyen"
git config user.email "tinnguyen8426@gmail.com"

echo Add tat ca cac file con thieu (respecting gitignore)...
git add .

echo Tao commit tong hop...
git commit -m "feat: integrate client frontend, services layer and final view boilerplate"

echo Day len cloud...
git push -u origin feature/final-integration --force

echo Mo Pull Request cuoi cung CHOT MON...
gh pr create --base dev --head feature/final-integration --title "feat: Final Project Synthesis & Documentation" --body "Day la ban gop cuoi cung de dong bo 100% local vao lich su Git. Bao gom README va Frontend."

echo Tra ve nhanh main de user thuc hanh merge...
git checkout main

echo XONG PHASE 5! MOI NHOM VAO MERGE PR #30!
