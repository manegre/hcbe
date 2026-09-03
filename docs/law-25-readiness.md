# Québec Law 25 readiness register

This register tracks technical support for privacy obligations. It is not a legal certification and should be reviewed by Québec privacy counsel and the HCBE Canada privacy officer.

## User-account controls

| Control | Implementation | Status |
| --- | --- | --- |
| Clear purposes and collection notice | Public privacy policy and membership/newsletter consent copy | Implemented; counsel review required |
| Privacy-protective defaults | Optional member communications, directory visibility, contact requests, and mentorship sharing default off | Implemented |
| Withdraw optional communications | Member preference centre provides granular choices and one-click opt-out; public newsletter records are deactivated too | Implemented |
| Access and portability | Authenticated structured JSON export | Implemented |
| Rectification | Member profile editing plus written-request channel | Implemented |
| Directory withdrawal | Member can hide profile and disable contact requests independently | Implemented |
| Account deletion | Authenticated request, 30-day cancellation period, automatic deactivation and anonymization | Implemented |
| User-uploaded files | Service-case attachments uploaded by the member are removed from object storage when deletion executes | Implemented |
| Request identity check | Current authenticated session for self-service; written requests require manual verification | Implemented / operational procedure required |
| Privacy contact | Privacy Officer title and `contact@hcbe.ca` published | Implemented; assign a named internal owner |

## Organizational actions still required

- Formally appoint the privacy officer and make sure `contact@hcbe.ca` is monitored with backup coverage.
- Approve a retention schedule and legal-hold rules for every record category, including production backups.
- Complete and retain privacy impact assessments for the platform and for transfers or processing outside Québec.
- Execute and periodically review data-processing/security terms for Railway, Brevo, Google, object storage, and any future provider.
- Maintain a confidentiality-incident register and a tested assessment/notification procedure for the Commission d’accès à l’information and affected people.
- Document how written access, correction, portability, consent-withdrawal, de-indexing, and complaint requests are verified, logged, answered, and closed within 30 days.
- Train administrators on minimum access, exports, deletion holds, incident escalation, and phishing/social-engineering risks.
- Review the public policy whenever purposes, fields, providers, analytics, cookies, retention, or cross-border processing changes.
- Schedule an annual Law 25 control review and retain evidence of the review.
