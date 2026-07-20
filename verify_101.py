import sqlite3

db = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin'
conn = sqlite3.connect(db)
cur = conn.cursor()

cur.execute("SELECT ContentAr, ContentEn, ContentFr FROM SubCategories WHERE Id=101")
ar, en, fr = cur.fetchone()
conn.close()

def show(label, text):
    sections = [s.strip() for s in text.replace('<h4>', '\n<h4>').split('\n') if s.strip()]
    print(f'\n--- {label} ---')
    for s in sections:
        if s.startswith('<h'):
            import re
            print(' ' + re.sub(r'<[^>]+>', '', s))

show('Arabic (contentAr)', ar)
show('English (contentEn)', en)
show('French (contentFr)', fr)
