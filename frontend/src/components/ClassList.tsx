import { useState, useEffect, useCallback } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Button,
  Typography,
  Chip,
  Alert,
  CircularProgress,
  Box,
} from '@mui/material';
import type { ClassInfo } from '../types';
import { getClasses, createHold } from '../api';

interface Props {
  studentId: string;
  onHoldCreated: () => void;
}

export default function ClassList({ studentId, onHoldCreated }: Props) {
  const [classes, setClasses] = useState<ClassInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [applyingClassId, setApplyingClassId] = useState<string | null>(null);
  const [applyError, setApplyError] = useState('');

  const fetchClasses = useCallback(async () => {
    try {
      const data = await getClasses();
      setClasses(data);
      setError('');
    } catch (err: any) {
      setError(err?.response?.data?.error || err?.message || 'Failed to load classes.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchClasses();
    // Auto-refresh every 5 seconds (requirement: seat counts update without page reload)
    const interval = setInterval(fetchClasses, 5000);
    return () => clearInterval(interval);
  }, [fetchClasses]);

  const handleApply = async (classId: string) => {
    if (!studentId) {
      setApplyError('Please enter your Birth Registration Number first.');
      return;
    }
    setApplyingClassId(classId);
    setApplyError('');
    try {
      await createHold(classId, studentId);
      onHoldCreated();
      await fetchClasses();
    } catch (err: any) {
      const msg = err?.response?.data?.error || err?.message || 'Failed to apply.';
      setApplyError(msg);
    } finally {
      setApplyingClassId(null);
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      {applyError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setApplyError('')}>
          {applyError}
        </Alert>
      )}

      {classes.length === 0 ? (
        <Typography color="text.secondary" sx={{ p: 2 }}>
          No classes available yet. An admin can create classes using the button above.
        </Typography>
      ) : (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow sx={{ backgroundColor: 'grey.100' }}>
                <TableCell><strong>Class Name</strong></TableCell>
                <TableCell align="center"><strong>Total Seats</strong></TableCell>
                <TableCell align="center"><strong>Held</strong></TableCell>
                <TableCell align="center"><strong>Confirmed</strong></TableCell>
                <TableCell align="center"><strong>Available</strong></TableCell>
                <TableCell align="center"><strong>Action</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {classes.map((cls) => (
                <TableRow key={cls.id} hover>
                  <TableCell>{cls.name}</TableCell>
                  <TableCell align="center">{cls.totalSeats}</TableCell>
                  <TableCell align="center">
                    <Chip label={cls.heldSeats} size="small" color="warning" variant="outlined" />
                  </TableCell>
                  <TableCell align="center">
                    <Chip label={cls.confirmedSeats} size="small" color="success" variant="outlined" />
                  </TableCell>
                  <TableCell align="center">
                    <Chip
                      label={cls.availableSeats}
                      size="small"
                      color={cls.availableSeats > 0 ? 'primary' : 'error'}
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Button
                      variant="contained"
                      size="small"
                      disabled={cls.availableSeats <= 0 || !studentId || applyingClassId === cls.id}
                      onClick={() => handleApply(cls.id)}
                    >
                      {applyingClassId === cls.id ? 'Applying...' : 'Apply'}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </>
  );
}
