CONTRACT MONTHLY CLAIM SYSTEM

It gives lecturers an all-inclusive way to upload papers and submit claims quickly
ClaimMonth and ClaimYear were added to the Claim and SubmitClaimViewModel.

Month and year options were added to the submission user interface.
The controller was updated to utilize the chosen month or year in place of the submission date.
Validation logic has been updated to check duplication by chosen month or year.
Unused methods have been updated to include new claim period fields.

ClaimDetails.cshtml now has a document download option.
The DownloadDocument controller action has been added.

HandleFileUpload was redesigned to simply process and return Document objects.


