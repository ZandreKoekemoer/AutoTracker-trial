import os
import sqlite3
import tempfile
from pathlib import Path
from playwright.sync_api import sync_playwright

BASE = os.environ.get("AUTOTRACKER_QA_URL", "http://127.0.0.1:5079")
EMAIL = os.environ["AUTOTRACKER_QA_EMAIL"]
PASSWORD = os.environ["AUTOTRACKER_QA_PASSWORD"]
DB = os.environ["AUTOTRACKER_QA_DB"]
OUT = Path(tempfile.gettempdir()) / "autotracker-qa"
OUT.mkdir(exist_ok=True)


def assert_no_overflow(page):
    metrics = page.evaluate("() => ({inner: innerWidth, scroll: document.documentElement.scrollWidth})")
    assert metrics["scroll"] == metrics["inner"], metrics


def assert_touch_targets(page):
    bad = page.evaluate("""() => [...document.querySelectorAll('a,button,input,select,textarea')]
      .filter(el => { const s=getComputedStyle(el), r=el.getBoundingClientRect(); const hit=(el.matches('input[type=checkbox],input[type=radio]')&&el.closest('label'))?el.closest('label').getBoundingClientRect():r; return s.display!=='none' && s.visibility!=='hidden' && r.width>0 && r.height>0 && (hit.height < 44 || hit.width < 44); })
      .map(el => ({tag:el.tagName,text:(el.innerText||el.getAttribute('aria-label')||el.name||'').trim().slice(0,50),w:Math.round(el.getBoundingClientRect().width),h:Math.round(el.getBoundingClientRect().height)}))""")
    assert not bad, bad


def login(page, email=EMAIL, password=PASSWORD):
    page.goto(f"{BASE}/Account/Login", wait_until="domcontentloaded")
    page.locator("#Email").fill(email)
    page.locator("#Password").fill(password)
    page.get_by_role("button", name="Sign in").click()
    page.wait_for_url(f"{BASE}/")


with sync_playwright() as pw:
    browser = pw.chromium.launch(
        executable_path=r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        headless=True,
        args=["--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream"]
    )
    errors = []

    for width in (320, 390, 430):
        context = browser.new_context(viewport={"width": width, "height": 760}, is_mobile=True, has_touch=True)
        page = context.new_page()
        page.on("console", lambda msg: errors.append(f"console:{msg.text}") if msg.type == "error" else None)
        page.on("pageerror", lambda err: errors.append(f"page:{err}"))
        page.goto(f"{BASE}/Account/Login", wait_until="domcontentloaded")
        assert page.locator(".login-card").is_visible()
        assert not page.locator(".sidebar").count()
        assert_no_overflow(page)
        assert_touch_targets(page)
        if width == 320:
            page.screenshot(path=str(OUT / "login-320.png"), full_page=True)
        context.close()

    context = browser.new_context(viewport={"width": 1440, "height": 900})
    page = context.new_page()
    page.on("console", lambda msg: errors.append(f"console:{msg.text}") if msg.type == "error" else None)
    page.on("pageerror", lambda err: errors.append(f"page:{err}"))
    page.goto(f"{BASE}/Account/Login", wait_until="domcontentloaded")
    page.locator("#Email").fill("missing-user@example.test")
    page.locator("#Password").fill("wrong-password")
    page.get_by_role("button", name="Sign in").click()
    assert "Invalid email or password." in page.locator("body").inner_text()
    assert "missing-user@example.test" not in page.locator(".validation-summary").inner_text()
    page.locator("#themeToggle").click()
    assert "light-mode" in page.locator("body").get_attribute("class")
    login(page)
    assert page.locator(".sidebar").is_visible()
    page.screenshot(path=str(OUT / "dashboard-1440.png"), full_page=True)

    response = page.request.post(f"{BASE}/Account/Logout")
    assert response.status == 400, response.status

    page.goto(f"{BASE}/RepairJobs/Details/1", wait_until="domcontentloaded")
    assert "Damage" not in page.locator(".card:has-text('Job Workspace')").inner_text()
    assert page.get_by_role("heading", name="Client Tracking Access").is_visible()
    assert_no_overflow(page)

    page.get_by_role("button", name="Generate New Link").click()
    page.wait_for_load_state("domcontentloaded")
    tracking_link = page.locator("#trackingLink").input_value()
    assert tracking_link.startswith(BASE + "/ClientTracker/Job?token=")

    public = browser.new_context(viewport={"width": 390, "height": 844}, is_mobile=True, has_touch=True)
    public_page = public.new_page()
    public_page.goto(tracking_link, wait_until="domcontentloaded")
    text = public_page.locator("body").inner_text()
    assert "Quote Total" not in text and "Profit" not in text and "Actual Cost" not in text
    assert not public_page.locator(".sidebar").count()
    assert_no_overflow(public_page)
    public_page.screenshot(path=str(OUT / "client-tracking-390.png"), full_page=True)

    page.get_by_role("button", name="Revoke Client Access").click()
    page.wait_for_load_state("domcontentloaded")
    public_page.goto(tracking_link, wait_until="domcontentloaded")
    assert "Tracking link unavailable" in public_page.locator("body").inner_text()

    page.goto(f"{BASE}/RepairJobs/Inventory/1", wait_until="domcontentloaded")
    assert page.get_by_role("heading", name="Add Part").is_visible()
    assert "Damage" not in page.locator("body").inner_text()

    with sqlite3.connect(DB) as con:
        candidate = con.execute("SELECT Id, Status FROM RepairJobs WHERE Status NOT IN ('Collected','Archived') ORDER BY Id LIMIT 1").fetchone()
    if candidate:
        job_id, original_status = candidate
        page.goto(f"{BASE}/RepairJobs/Details/{job_id}", wait_until="domcontentloaded")
        csrf = page.locator('input[name="__RequestVerificationToken"]').first.input_value()
        response = page.request.post(f"{BASE}/RepairJobs/UpdateStatus", form={
            "id": str(job_id), "status": "Collected", "notifyClient": "false",
            "clientNote": "", "__RequestVerificationToken": csrf
        })
        assert response.ok, response.status
        with sqlite3.connect(DB) as con:
            assert con.execute("SELECT Status FROM RepairJobs WHERE Id=?", (job_id,)).fetchone()[0] == original_status

    page.goto(f"{BASE}/RfidTags", wait_until="domcontentloaded")
    assert page.get_by_role("button", name="Start Camera").is_visible()
    assert page.get_by_role("button", name="Open Assigned Job").is_visible()
    page.set_viewport_size({"width": 430, "height": 844})
    assert_no_overflow(page)
    page.screenshot(path=str(OUT / "rfid-430.png"), full_page=True)

    manifest = page.request.get(f"{BASE}/manifest.webmanifest")
    assert manifest.ok and "AutoTracker" in manifest.text()
    worker = page.request.get(f"{BASE}/service-worker.js")
    assert worker.ok and "STATIC_CACHE" in worker.text()

    with sqlite3.connect(DB) as con:
        row = con.execute("SELECT FilePath FROM JobPhotos ORDER BY Id LIMIT 1").fetchone()
    if row:
        anonymous = public.request.get(BASE + row[0])
        assert anonymous.status == 404, anonymous.status
        authenticated = page.request.get(BASE + row[0])
        assert authenticated.ok, authenticated.status

    with sqlite3.connect(DB) as con:
        admin_hash = con.execute("SELECT PasswordHash FROM AppUsers WHERE Email=?", (EMAIL,)).fetchone()[0]
        con.execute("DELETE FROM AppUsers WHERE Email='browser-tech@example.test'")
        cur = con.execute("INSERT INTO AppUsers(FullName,Email,PasswordHash,Role,CompanyId,IsActive,SessionVersion,CreatedAt) VALUES(?,?,?,?,?,1,1,datetime('now'))", ("Browser Technician", "browser-tech@example.test", admin_hash, "Technician", 1))
        tech_id = cur.lastrowid
        own = con.execute("SELECT r.Id, v.RegNumber, p.FilePath, r.AssignedTechnicianUserId, r.AssignedTechnician FROM RepairJobs r JOIN Vehicles v ON v.Id=r.VehicleId JOIN JobPhotos p ON p.RepairJobId=r.Id WHERE r.Status NOT IN ('Collected','Archived') GROUP BY r.Id ORDER BY r.Id LIMIT 1").fetchone()
        other = con.execute("SELECT r.Id, v.RegNumber, p.FilePath, r.AssignedTechnicianUserId, r.AssignedTechnician FROM RepairJobs r JOIN Vehicles v ON v.Id=r.VehicleId JOIN JobPhotos p ON p.RepairJobId=r.Id WHERE r.Id<>? GROUP BY r.Id ORDER BY r.Id LIMIT 1", (own[0] if own else -1,)).fetchone()
        jobs = [item for item in (own, other) if item is not None]
        if len(jobs) >= 2:
            con.execute("UPDATE RepairJobs SET AssignedTechnicianUserId=?, AssignedTechnician=? WHERE Id=?", (tech_id, "Browser Technician", jobs[0][0]))
        con.commit()

    if len(jobs) >= 2:
        tech_context = browser.new_context(viewport={"width": 390, "height": 844}, is_mobile=True, has_touch=True)
        tech_page = tech_context.new_page()
        tech_page.on("console", lambda msg: errors.append(f"tech-console:{msg.text}") if msg.type == "error" else None)
        login(tech_page, "browser-tech@example.test", PASSWORD)
        home_text = tech_page.locator("body").inner_text()
        assert jobs[0][1] in home_text
        assert jobs[1][1] not in home_text
        assert tech_page.request.get(BASE + jobs[0][2]).ok
        assert tech_page.request.get(BASE + jobs[1][2]).status == 404
        tech_context.close()
        with sqlite3.connect(DB) as con:
            con.execute("UPDATE RepairJobs SET AssignedTechnicianUserId=?, AssignedTechnician=? WHERE Id=?", (jobs[0][3], jobs[0][4], jobs[0][0]))
            con.execute("DELETE FROM AppUsers WHERE Id=?", (tech_id,))
            con.commit()

    with sqlite3.connect(DB) as con:
        con.execute("UPDATE AppUsers SET SessionVersion = SessionVersion + 1 WHERE Email = ?", (EMAIL,))
        con.commit()
    page.goto(f"{BASE}/", wait_until="domcontentloaded")
    assert "/Account/Login" in page.url

    assert not errors, errors
    public.close()
    context.close()
    browser.close()

print(f"browser-qa-passed screenshots={OUT}")
