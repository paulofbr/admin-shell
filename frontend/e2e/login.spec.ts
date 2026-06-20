import { test, expect } from '@playwright/test'

test.describe('Admin Shell - Login', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173')
  })

  test('displays login page', async ({ page }) => {
    await expect(page.locator('h2')).toContainText(/sign in|login/i)
    await expect(page.locator('input[type="email"]')).toBeVisible()
    await expect(page.locator('input[type="password"]')).toBeVisible()
    await expect(page.locator('button[type="submit"]')).toBeVisible()
  })

  test('shows error on invalid login', async ({ page }) => {
    await page.fill('input[type="email"]', 'invalid@test.com')
    await page.fill('input[type="password"]', 'wrongpassword')
    await page.click('button[type="submit"]')

    await expect(page.locator('.el-message--error')).toBeVisible({ timeout: 10000 })
  })

  test('successful login navigates to dashboard', async ({ page }) => {
    await page.fill('input[type="email"]', 'admin@admin.com')
    await page.fill('input[type="password"]', 'admin123')
    await page.click('button[type="submit"]')

    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 })
    await expect(page.locator('.dashboard-container')).toBeVisible()
  })

  test('logout returns to login page', async ({ page }) => {
    // Login first
    await page.fill('input[type="email"]', 'admin@admin.com')
    await page.fill('input[type="password"]', 'admin123')
    await page.click('button[type="submit"]')
    await page.waitForURL(/\/dashboard/)

    // Click logout
    await page.click('[data-testid="logout-button"]')
    await expect(page).toHaveURL(/\/login/)
  })
})
