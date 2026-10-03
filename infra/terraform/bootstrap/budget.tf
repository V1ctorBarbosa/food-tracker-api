# A safety net for staying on the AWS Paid Plan without actually paying anything: alerts by email
# the moment real spend appears. Only an alert, not an automatic block -- the Paid Plan removes the
# Free Plan's account-wide suspend-on-overrun behavior, so this is what replaces it. Always Free
# services (Lambda included) stay free under their published caps, so this should only ever fire
# from a genuine misconfiguration (e.g. a runaway request loop), not normal usage.
resource "aws_budgets_budget" "zero_spend" {
  name              = "food-track-zero-spend"
  budget_type       = "COST"
  limit_amount      = "0.01"
  limit_unit        = "USD"
  time_unit         = "MONTHLY"
  time_period_start = "2026-01-01_00:00"

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 100
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_email_addresses = [var.budget_alert_email]
  }
}
