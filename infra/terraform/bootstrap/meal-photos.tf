# Photos of meals estimated by AI. Lives in bootstrap, not in the app config, because the GitHub
# Actions user may only manage the Lambda function: creating a bucket or granting the function
# access to one stays a hand-applied change.
#
# Layout (see MealPhotoKeys in the API): pending/{userId}/{captureId}.jpg for photos uploaded for
# an estimate that may never be saved, attached/{userId}/{captureId}.jpg once a meal keeps them.
resource "aws_s3_bucket" "meal_photos" {
  # Bucket names are global across every AWS account, so the account id keeps this one unique.
  bucket = "food-track-meal-photos-${data.aws_caller_identity.current.account_id}"
}

resource "aws_s3_bucket_public_access_block" "meal_photos" {
  bucket = aws_s3_bucket.meal_photos.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_ownership_controls" "meal_photos" {
  bucket = aws_s3_bucket.meal_photos.id

  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "meal_photos" {
  bucket = aws_s3_bucket.meal_photos.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# An estimate the user never confirms leaves its photo under pending/. Saving a meal moves the photo
# to attached/, which no rule touches; deleting the meal or the account deletes it explicitly.
resource "aws_s3_bucket_lifecycle_configuration" "meal_photos" {
  bucket = aws_s3_bucket.meal_photos.id

  rule {
    id     = "expire-unconfirmed-estimate-photos"
    status = "Enabled"

    filter {
      prefix = "pending/"
    }

    expiration {
      days = 1
    }
  }

  rule {
    id     = "abort-incomplete-uploads"
    status = "Enabled"

    filter {}

    abort_incomplete_multipart_upload {
      days_after_initiation = 1
    }
  }
}

# The function signs upload and read URLs with its own role, so the role needs every action those
# URLs perform (PutObject, GetObject) plus the ones the API runs itself (HEAD through GetObject,
# copy, delete, and listing a user's prefix when an account is deleted).
data "aws_iam_policy_document" "lambda_meal_photos" {
  statement {
    sid       = "ReadAndWriteMealPhotos"
    actions   = ["s3:PutObject", "s3:GetObject", "s3:DeleteObject"]
    resources = ["${aws_s3_bucket.meal_photos.arn}/*"]
  }

  statement {
    sid       = "ListMealPhotos"
    actions   = ["s3:ListBucket"]
    resources = [aws_s3_bucket.meal_photos.arn]
  }
}

resource "aws_iam_role_policy" "lambda_meal_photos" {
  name   = "read-write-food-track-meal-photos"
  role   = aws_iam_role.lambda_execution.name
  policy = data.aws_iam_policy_document.lambda_meal_photos.json
}
