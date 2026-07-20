import sqlite3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
db_path = root / "KhayratAlhaj" / "Resources" / "Data" / "appdata.bin"

con = sqlite3.connect(db_path)
cur = con.cursor()

print("categories_count", cur.execute("SELECT count(*) FROM Categories").fetchone()[0])
print("subcategories_count", cur.execute("SELECT count(*) FROM SubCategories").fetchone()[0])
print("cat7_count", cur.execute("SELECT count(*) FROM SubCategories WHERE CategoryId=7").fetchone()[0])
print("cat8_count", cur.execute("SELECT count(*) FROM SubCategories WHERE CategoryId=8").fetchone()[0])

missing_names = cur.execute(
    """
    SELECT count(*)
    FROM SubCategories
    WHERE CategoryId IN (7,8)
      AND (ifnull(NameAr,'')='' OR ifnull(NameEn,'')='' OR ifnull(NameFr,'')='')
    """
).fetchone()[0]
print("missing_names", missing_names)

missing_content = cur.execute(
    """
    SELECT count(*)
    FROM SubCategories
    WHERE CategoryId IN (7,8)
      AND (ifnull(ContentAr,'')='' OR ifnull(ContentEn,'')='' OR ifnull(ContentFr,'')='')
    """
).fetchone()[0]
print("missing_content", missing_content)

audio_zero = cur.execute(
  "SELECT count(*) FROM SubCategories WHERE CategoryId IN (7,8) AND ifnull(HasAudioAr,0)=0"
).fetchone()[0]
print("audio_zero", audio_zero)

con.close()
