---
name: invite-entra-guest
description: Invite an external user to the Microsoft Entra ID tenant as a guest so they can sign in to BreakRetailManager. Use this when asked to add, invite, or onboard a new user.
---

## Invite an external user via Azure CLI

Replace `$email` with the user's email address.

```powershell
$tenantId = "1b0dba22-92b1-49e7-a4f2-1bd7a1c45202"
$email = "user@example.com"
$redirectUrl = "https://myapps.microsoft.com"

# Ensure logged in to the correct tenant
az login --tenant $tenantId --use-device-code --allow-no-subscriptions

# Build the invitation payload
$body = @{
  invitedUserEmailAddress = $email
  inviteRedirectUrl       = $redirectUrl
  sendInvitationMessage   = $true
} | ConvertTo-Json -Compress

# az rest requires a file for the JSON body to avoid encoding issues
$tmp = New-TemporaryFile
Set-Content -Path $tmp -Value $body -NoNewline -Encoding utf8
az rest --method post `
  --url "https://graph.microsoft.com/v1.0/invitations" `
  --resource "https://graph.microsoft.com" `
  --headers "Content-Type=application/json" `
  --body "@$tmp" `
  --output json
Remove-Item $tmp -Force
```

## Expected response

The response includes `status: PendingAcceptance` and `invitedUserType: Guest`.

## Post-invitation steps

1. The invited user receives an email and must **accept the invitation**.
2. If sign-in fails with **AADSTS50105**, assign the user in **Entra ID → Enterprise applications → Users and groups** for the app registration.
3. On first sign-in the app auto-provisions the user via `GET /api/users/me`.
4. An admin can then assign app roles (Admin, Manager, Cashier) in the **`/admin/users`** page.
