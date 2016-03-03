#!/usr/bin/env python

"""Create smaller GSFLOW PRMS data file to use in unit tests."""

import os

__author__ = 'Bob Lounsbury'
__email__ = 'blounsbury@usbr.gov'
__date__ = '03/02/2016'


def main():
    with open('deschutes.1980-2013.data') as f:
        content = f.readlines()

    i = 0
    new_lines = []
    while '####' not in content[i]:
        line = content[i]
        if 'tmax' in line:
            line = 'tmax 5\n'
        elif 'tmin' in line:
            line = 'tmin 5\n'
        elif 'precip' in line:
            line = 'precip 5\n'
        elif 'runoff' in line:
            line = 'runoff 1\n'
        new_lines.append(line)
        i += 1
    new_lines.append(content[i])

    for j in xrange(i + 1, len(content)):
        line = content[j].split(' ')

        # add date
        last_idx = 6
        new_line = line[:last_idx]

        # add 5 tmax values
        new_line.extend(line[last_idx:last_idx + 5])

        # add 5 tmin values
        last_idx += 587
        new_line.extend(line[last_idx:last_idx + 5])

        # add 5 precip values
        last_idx += 587
        new_line.extend(line[last_idx:last_idx + 5])

        # add 1 runoff value
        last_idx += 587
        new_line.extend(line[last_idx:last_idx + 1])

        new_lines.append(' '.join(new_line) + '\n')

    with open('deschutes.1980-2013.small.data', 'w') as f:
        f.writelines(new_lines)


if __name__ == '__main__':
    main()
