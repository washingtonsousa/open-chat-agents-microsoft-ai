"use client";

import { useEffect, useState } from "react";
import { Autocomplete, Chip, TextField } from "@mui/material";
import AutoAwesomeIcon from "@mui/icons-material/AutoAwesome";
import { skillApi } from "@/services/api";
import type { Skill } from "@/types";

interface Props {
  value: string[];
  onChange: (ids: string[]) => void;
}

export function SkillMultiSelect({ value, onChange }: Props) {
  const [options, setOptions] = useState<Skill[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    skillApi
      .list()
      .then((res) => setOptions(res.skills))
      .finally(() => setLoading(false));
  }, []);

  const selected = options.filter((skill) => value.includes(skill.id));

  return (
    <Autocomplete
      multiple
      loading={loading}
      options={options}
      value={selected}
      getOptionLabel={(skill) => skill.name}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      onChange={(_, newValue) => onChange(newValue.map((skill) => skill.id))}
      noOptionsText="Nenhuma skill criada ainda."
      renderValue={(tagValue, getItemProps) =>
        tagValue.map((skill, index) => {
          const { key, ...itemProps } = getItemProps({ index });
          return (
            <Chip
              {...itemProps}
              key={key}
              label={skill.name}
              size="small"
              icon={<AutoAwesomeIcon />}
              color="success"
              variant="outlined"
            />
          );
        })
      }
      renderInput={(params) => (
        <TextField
          {...params}
          placeholder={selected.length === 0 ? "Adicionar skill..." : undefined}
          size="small"
        />
      )}
    />
  );
}
