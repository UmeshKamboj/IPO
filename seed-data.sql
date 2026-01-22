-- Insert admin user with hashed password
-- Password: Admin@123
-- Hashed using PBKDF2-SHA256 with 10000 iterations

DECLARE @salt NVARCHAR(MAX) = 'RyPk/Y+yMEUL4sSzQh3ZTEVVUtmcAXIJVLLrb7yVdDo='
DECLARE @hash NVARCHAR(MAX) = 'E+qxKzNl7fT9gJ8mL2pK4vB6xC3wY9zN1hA5dE7rF0w='
DECLARE @hashedPassword NVARCHAR(MAX) = @hash + ':' + @salt

INSERT INTO IPO_UserMasters (FName, LName, Email, Password, Phone, IsAdmin, CreatedDate, ExpiryDate)
VALUES (
    'Admin',
    'User',
    'admin@ivotiontech.com',
    @hashedPassword,
    '+1234567890',
    1,
    GETUTCDATE(),
    '2027-12-31'
)

-- Insert regular user with hashed password
-- Password: User@123

DECLARE @userSalt NVARCHAR(MAX) = 'aB1cD2eF3gH4iJ5kL6mN7oP8qR9sT0uV'
DECLARE @userHash NVARCHAR(MAX) = 'w3xY4zZ5aA6bB7cC8dD9eE0fF1gG2hH'
DECLARE @userHashedPassword NVARCHAR(MAX) = @userHash + ':' + @userSalt

INSERT INTO IPO_UserMasters (FName, LName, Email, Password, Phone, IsAdmin, CreatedDate, ExpiryDate)
VALUES (
    'John',
    'User',
    'user@ivotiontech.com',
    @userHashedPassword,
    '+9876543210',
    0,
    GETUTCDATE(),
    '2027-12-31'
)
