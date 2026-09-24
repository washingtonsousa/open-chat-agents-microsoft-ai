"use client";

import { Dialog, DialogContent, DialogTitle, IconButton } from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import type { Skill } from "@/types";
import { SkillForm } from "./SkillForm";

interface Props {
  onSaved: (skill: Skill) => void;
  onClose: () => void;
  editing?: Skill;
}

export function SkillModal({ onSaved, onClose, editing }: Props) {
  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        {editing ? "Editar skill" : "Nova skill"}
        <IconButton onClick={onClose} size="small">
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <SkillForm onSaved={onSaved} onCancel={onClose} editing={editing} />
      </DialogContent>
    </Dialog>
  );
}
