# ADR 0002: Preserve original project bytes

Status: implemented in the portable/recovery format.

MPP compatibility is operation-dependent. Retain the original bytes and explicit replayable edits in portable/recovery packages rather than flattening every project to XML. Validate bounded schemas and command allowlists, reconstruct in isolation and then replace the live session. Original/existing destinations remain protected. This improves preservation but does not prove unrestricted MPP compatibility.
