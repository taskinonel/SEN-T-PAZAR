#!/usr/bin/env python3
import sqlite3,subprocess,sys,os,uuid,tempfile
# ensure temp files are world-readable so 'postgres' user can read them
os.umask(0o022)
SQLITE_DB='/var/backups/sentpazar/database/sent-pazar-20260525-160644.db'
EMAIL='taskinonel@gmail.com'
TMP_DIR='/tmp/sent_migrate'
os.makedirs(TMP_DIR,exist_ok=True)
con=sqlite3.connect(SQLITE_DB)
con.row_factory=sqlite3.Row
cur=con.cursor()
# fetch user from sqlite
try:
    cur.execute('SELECT * FROM "AspNetUsers" WHERE lower(email)=lower(?)',(EMAIL,))
    user_row=cur.fetchone()
except Exception as e:
    print('SQLITE_ERROR',e)
    sys.exit(5)
if not user_row:
    print('NO_USER_IN_SQLITE')
    sys.exit(2)
user=dict(user_row)
print('SQLITE_USER_ID',user.get('Id'))
# helper to run psql as postgres and capture output
import tempfile

def run_psql_file(sql_text, capture=False):
    fd, path = tempfile.mkstemp(dir=TMP_DIR, text=True)
    with os.fdopen(fd, 'w', encoding='utf-8') as f:
        f.write(sql_text)
    try:
        os.chmod(path,0o644)
    except Exception:
        pass
    cmd=['sudo','-u','postgres','psql','-d','sentpazar','-v','ON_ERROR_STOP=1','-q','-f',path]
    if capture:
        res=subprocess.run(cmd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
        os.remove(path)
        return res
    else:
        res=subprocess.run(cmd)
        os.remove(path)
        return res

def run_psql_cmd(cmd_text, capture=False):
    sql = cmd_text
    return run_psql_file(sql, capture=capture)

# get postgres columns for a table
def get_pg_columns(table):
    q=f"SELECT a.attname FROM pg_attribute a JOIN pg_class c ON a.attrelid=c.oid WHERE c.relname='{table}' AND a.attnum>0 AND NOT a.attisdropped;"
    r=run_psql_cmd(q, capture=True)
    if r.returncode!=0:
        print('PSQL_ERROR',r.stderr)
        return []
    cols=[l.strip() for l in r.stdout.splitlines() if l.strip()]
    return cols

# get sqlite columns for a table
def get_sqlite_columns(table):
    cur2=con.execute(f"PRAGMA table_info(\"{table}\");")
    return [r['name'] for r in cur2.fetchall()]

# fetch listings belonging to sqlite user id
sqlite_user_id=user.get('Id')
if not sqlite_user_id:
    print('USER_HAS_NO_ID')
    sys.exit(3)
cur.execute('SELECT * FROM "Listings" WHERE "UserId"=?',(sqlite_user_id,))
listings=cur.fetchall()
print('FOUND_LISTINGS',len(listings))
# get column intersection for AspNetUsers and Listings
pg_user_cols=get_pg_columns('AspNetUsers')
pg_listing_cols=get_pg_columns('Listings')
sqlite_user_cols=get_sqlite_columns('AspNetUsers')
sqlite_listing_cols=get_sqlite_columns('Listings')
pg_user_set=set(pg_user_cols)
pg_listing_set=set(pg_listing_cols)
user_cols=[c for c in sqlite_user_cols if c in pg_user_set]
listing_cols=[c for c in sqlite_listing_cols if c in pg_listing_set]
print('USER_COLS_TO_COPY',user_cols)
print('LISTING_COLS_TO_COPY',listing_cols)
# check if user exists in pg
r=run_psql_cmd(f"SELECT id FROM \"AspNetUsers\" WHERE lower(email)=lower('{EMAIL}') LIMIT 1;",capture=True)
pg_user_id=None
if r.returncode==0 and r.stdout.strip():
    pg_user_id=r.stdout.strip().splitlines()[-1].strip()
    print('PG_USER_EXISTS',pg_user_id)
else:
    # insert user
    new_id=str(uuid.uuid4())
    cols=[]
    vals=[]
    for c in user_cols:
        cols.append(f'"{c}"')
        v=user.get(c)
        if v is None:
            vals.append('NULL')
        else:
            s=str(v).replace('\\','\\\\').replace("'","''")
            vals.append("'"+s+"'")
    # ensure Id present
    if 'Id' not in user_cols:
        cols.insert(0,'"Id"')
        vals.insert(0,"'"+new_id+"'")
    insert_sql=f'INSERT INTO "AspNetUsers" ({",".join(cols)}) VALUES ({",".join(vals)});'
    print('INSERT_USER_SQL',insert_sql[:400])
    r2=run_psql_cmd(insert_sql,capture=True)
    if r2.returncode!=0:
        print('PG_INSERT_USER_ERROR',r2.stderr)
        sys.exit(4)
    pg_user_id=new_id
    print('CREATED_PG_USER',pg_user_id)
# now insert listings
import time
if listings:
    for row in listings:
        rowd=dict(row)
        rowd['UserId']=pg_user_id
        cols=[]
        vals=[]
        for c in listing_cols:
            cols.append(f'"{c}"')
            v=rowd.get(c)
            if v is None:
                vals.append('NULL')
            else:
                s=str(v).replace('\\','\\\\').replace("'","''")
                vals.append("'"+s+"'")
        # avoid huge SQL, execute per row
        set_clause = ', '.join([f'{col}=EXCLUDED.{col}' for col in cols if col!='"Id"'])
        insert=f'INSERT INTO "Listings" ({",".join(cols)}) VALUES ({",".join(vals)}) '
        if '"Id"' in cols:
            insert += f'ON CONFLICT ("Id") DO UPDATE SET {set_clause};'
        else:
            insert += ';'
        r3=run_psql_cmd(insert,capture=True)
        if r3.returncode!=0:
            print('PG_INSERT_LISTING_ERROR',r3.stderr)
            # continue to next
        else:
            print('IMPORTED_LISTING',rowd.get('Id'))
        time.sleep(0.05)
    print('LISTINGS_IMPORTED',len(listings))
# final dump generation
print('CREATING_POST_IMPORT_DUMP')
subprocess.run(['sudo','-u','postgres','pg_dump','-Fc','-d','sentpazar','-f','/tmp/post_import_sentpazar.dump'])
print('DUMP_CREATED:/tmp/post_import_sentpazar.dump')
print('DONE')
